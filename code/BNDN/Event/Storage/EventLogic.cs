﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Common;
using Event.Interfaces;
using Event.Models;

namespace Event.Storage
{
    public class EventLogic : IEventLogic
    {
        #region Properties
        public Uri OwnUri
        {
            set { Storage.OwnUri = value; }
            get { return Storage.OwnUri; }
        }

        public string WorkflowId
        {
            set { Storage.WorkflowId = value; }
            get { return Storage.WorkflowId; }
        }

        public string EventId
        {
            set { Storage.EventId = value; }
            get { return Storage.EventId; }
        }

        public string Name
        {
            set { Storage.Name = value; }
            get { return Storage.Name; }
        }

        public LockDto LockDto
        {
            set { Storage.LockDto = value; }
            get { return Storage.LockDto; }
        }

        public bool Executed
        {
            set { Storage.Executed = value; }
            get { return Storage.Executed; }
        }

        public bool Included
        {
            set { Storage.Included = value; }
            get { return Storage.Included; }
        }

        public bool Pending
        {
            set { Storage.Pending = value; }
            get { return Storage.Pending; }
        }

        public HashSet<RelationToOtherEventModel> Conditions
        {
            set { Storage.Conditions = value; }
            get { return Storage.Conditions; }
        }

        public HashSet<RelationToOtherEventModel> Responses
        {
            set { Storage.Responses = value; }
            get { return Storage.Responses; }
        }

        public HashSet<RelationToOtherEventModel> Exclusions
        {
            set { Storage.Exclusions = value; }
            get { return Storage.Exclusions; }
        }

        public HashSet<RelationToOtherEventModel> Inclusions
        {
            set { Storage.Inclusions = value; }
            get { return Storage.Inclusions; }
        }

        public IEnumerable<RelationToOtherEventModel> RelationsToLock
        {
            get
            {
                var resp = new HashSet<RelationToOtherEventModel>(Responses);
                var incl = new HashSet<RelationToOtherEventModel>(Inclusions);
                var excl = new HashSet<RelationToOtherEventModel>(Exclusions);
                return new List<RelationToOtherEventModel>(resp.Concat(incl.Concat(excl)));
            }
        }

        //The role that a given user has to have for execution.
        public string Role
        {
            get { return Storage.Role; }
            set { Storage.Role = value; }
        }

        #endregion

        #region Init
        //Storage instance for getting and setting data.
        private readonly IEventStorage Storage;

        /// <summary>
        /// Constructor for EventLogic
        /// </summary>
        /// <param name="eventId">The id of the event, that this EventLogic instance should represent</param>
        public EventLogic(string eventId)
        {
            Storage = new EventStorage(eventId, new EventContext());
        }
        public EventLogic(IEventStorage storage)
        {
            Storage = storage;
        }

        #endregion

        #region Rule Handling

        public void UnlockEvent()
        {
            Storage.ClearLock();
        }

        public async Task<bool> IsExecutable()
        {
            //If this event is excluded, return false.
            if (!Included)
            {
                return false;
            }

            foreach (var condition in Conditions)
            {
                IEventFromEvent eventCommunicator = new EventCommunicator(condition.Uri, condition.EventID, EventId);
                var executed = await eventCommunicator.IsExecuted();
                var included = await eventCommunicator.IsIncluded();
                // If the condition-event is not executed and currently included.
                if (!executed || included)
                {
                    return false; //Don't check rest because this event is not executable.
                }
            }

            return true; // If all conditions are executed or excluded.
        }

        public async Task UpdateRules(string id, EventRuleDto rules)
        {
            await Task.Run(() =>
            {
                //Todo : Persist this stuff.

                if (rules == null)
                {
                    throw new ArgumentNullException("rules","Provided rules was null");
                }
                var relation = new RelationToOtherEventModel {EventID = rules.Id, Uri = rules.Uri};
                UpdateRule(rules.Condition, relation, Conditions);
                UpdateRule(rules.Exclusion, relation, Exclusions);
                UpdateRule(rules.Inclusion, relation, Inclusions);
                UpdateRule(rules.Response, relation, Responses);
            });
        }

        private static void UpdateRule(bool shouldAdd, RelationToOtherEventModel value, ISet<RelationToOtherEventModel> collection)
        {
            //Todo : Persist this stuff.
            if (shouldAdd) collection.Add(value);
            else collection.Remove(value);
        }

        #endregion

        #region DTO Creation

        public Task<EventStateDto> EventStateDto
        {
            get
            {
                return Task.Run(async () => new EventStateDto
                {
                    Id = EventId,
                    Name = Name,
                    Executed = Executed,
                    Included = Included,
                    Pending = Pending,
                    Executable = await IsExecutable()
                });
            }
        }

        public Task<EventDto> EventDto
        {
            get
            {
                return Task.Run(() => new EventDto
                {
                    EventId = EventId,
                    WorkflowId = WorkflowId,
                    Name = Name,
                    Role = Role,
                    Pending = Pending,
                    Executed = Executed,
                    Included = Included,
                    Conditions = Conditions.Select(model => new EventAddressDto{Id = model.EventID,Uri = model.Uri}),
                    Exclusions = Exclusions.Select(model => new EventAddressDto { Id = model.EventID, Uri = model.Uri }),
                    Responses = Responses.Select(model => new EventAddressDto { Id = model.EventID, Uri = model.Uri }),
                    Inclusions = Inclusions.Select(model => new EventAddressDto { Id = model.EventID, Uri = model.Uri })
                });
            }
        }
        #endregion

        public async Task ResetState()
        {
            await Task.Run(() =>
            {
                Storage.Name = null;
                Storage.EventId = null;
                Storage.WorkflowId = null;
                Storage.OwnUri = null;
                Storage.Executed = false;
                Storage.Included = false;
                Storage.Pending = false;
            });
        }


        public bool CallerIsAllowedToOperate(EventAddressDto eventAddressDto)
        {
            if (LockDto == null) return true;
            // TODO: Consider implementing an Equals() method on EventAddressDto
            return LockDto.LockOwner.Equals(eventAddressDto.Id);
        }

        // TODO: InitializeEvent and UpdateEvent has a lot of duplicated code, will look into this later
        public async Task InitializeEvent(EventDto eventDto, Uri ownUri)
        {
            if (eventDto == null)
            {
                throw new ArgumentNullException("eventDto", "Provided EventDto was null");
            }
            if (!eventDto.EventId.Equals(EventId))
            {
                throw new ArgumentException("EventIds does not match!", "eventDto");
            }

            // #1. Make sure that server will accept our entry
            var dto = new EventAddressDto
            {
                Id = eventDto.EventId,
                Uri = ownUri
            };

            var serverCommunicator = new ServerCommunicator("http://localhost:13768/", eventDto.EventId, eventDto.WorkflowId);
            var otherEvents = await serverCommunicator.PostEventToServer(dto);

            try
            {
                // Setup a new Event in database.
                Storage.InitializeNewEvent();

                // #2. Then set our own fields accordingly
                EventId = eventDto.EventId;
                WorkflowId = eventDto.WorkflowId;
                Name = eventDto.Name;
                Role = eventDto.Role;
                Included = eventDto.Included;
                Role = eventDto.Role;
                Pending = eventDto.Pending;
                Executed = eventDto.Executed;
                Inclusions = new HashSet<RelationToOtherEventModel>(eventDto.Inclusions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
                Exclusions = new HashSet<RelationToOtherEventModel>(eventDto.Exclusions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
                Conditions = new HashSet<RelationToOtherEventModel>(eventDto.Conditions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
                Responses = new HashSet<RelationToOtherEventModel>(eventDto.Responses.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
                OwnUri = ownUri;
            }
            catch (Exception)
            {
                // if something goes wrong, we have to delete the event from the server again.
                serverCommunicator.DeleteEventFromServer().Wait();
                throw;
            }
            
        }

        public async Task UpdateEvent(EventDto eventDto, Uri ownUri)
        {
            if (eventDto == null)
            {
                throw new NullReferenceException("Provided EventDto was null");
            }
            if (EventId == null)
            {
                throw new NullReferenceException("EventId was null");
            }

            if (EventId != eventDto.EventId || WorkflowId != eventDto.WorkflowId)
            {
                //TODO: Remove from server and add again. Maybe remove.
            }

            EventId = eventDto.EventId;
            WorkflowId = eventDto.WorkflowId;
            Name = eventDto.Name;
            Included = eventDto.Included;
            Pending = eventDto.Pending;
            Executed = eventDto.Executed;
            Inclusions = new HashSet<RelationToOtherEventModel>(eventDto.Inclusions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
            Exclusions = new HashSet<RelationToOtherEventModel>(eventDto.Exclusions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
            Conditions = new HashSet<RelationToOtherEventModel>(eventDto.Conditions.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));
            Responses = new HashSet<RelationToOtherEventModel>(eventDto.Responses.Select(addressDto => new RelationToOtherEventModel { EventID = addressDto.Id, Uri = addressDto.Uri }));

           

            // Todo: This should not be necessary..
            OwnUri = ownUri;
        }

        public async Task DeleteEvent()
        {
            if (EventId == null)
            {
                throw new NullReferenceException();
            }
            var serverCommunicator = new ServerCommunicator("http://localhost:13768/", EventId, WorkflowId);
            await serverCommunicator.DeleteEventFromServer();
            await ResetState();
            Storage.Conditions = new HashSet<RelationToOtherEventModel>();
            Storage.Responses = new HashSet<RelationToOtherEventModel>();
            Storage.Inclusions = new HashSet<RelationToOtherEventModel>();
            Storage.Exclusions = new HashSet<RelationToOtherEventModel>();
        }

        public async Task Execute()
        {
            Executed = true;
            Pending = false;
            var addressDto = new EventAddressDto {Id = EventId, Uri = OwnUri};
            await Task.Run(async () =>
            {
                foreach (var pending in Responses)
                {
                    await new EventCommunicator(pending.Uri,pending.EventID,EventId).SendPending(true, addressDto);
                }
                foreach (var inclusion in Inclusions)
                {
                    await new EventCommunicator(inclusion.Uri, inclusion.EventID, EventId).SendIncluded(true, addressDto);
                }
                foreach (var exclusion in Exclusions)
                {
                    await new EventCommunicator(exclusion.Uri, exclusion.EventID, EventId).SendIncluded(false, addressDto);
                }
            });
        }

        public bool IsLocked()
        {
            return LockDto != null;
        }

        public void Dispose()
        {
            Storage.Dispose();
        }
    }
}