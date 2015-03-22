﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Event.Interfaces;

namespace Event.Models
{
    /// <summary>
    /// EventCommunicator handles the outgoing communication from an Event to another.
    /// </summary>
    public class EventCommunicator : IEventFromEvent
    {
        private readonly AwiaHttpClientToolbox _httpClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventUri">The base-address of the Event whose rules are to be deleted</param>
        public EventCommunicator(Uri eventUri)
        {
            if (AwiaHttpClientToolbox.IdHttpClientMap != null && AwiaHttpClientToolbox.IdHttpClientMap.ContainsKey(eventUri.ToString()))
            {
                _httpClient = AwiaHttpClientToolbox.IdHttpClientMap[eventUri.ToString()];
            }
            else
            {
                _httpClient = new AwiaHttpClientToolbox(eventUri);
            }
        }

        public async Task<bool> IsExecuted()
        {
            return await _httpClient.Read<bool>("event/executed");
        }

        public async Task<bool> IsIncluded()
        {
            return await _httpClient.Read<bool>("event/included");
        }

        /// <summary>
        /// GetEvent will return a representation of the Event asked for
        /// </summary>
        /// <returns>A Task object revealing af EventDto object</returns>
        public async Task<EventDto> GetEvent()
        {
            return await _httpClient.Read<EventDto>("");
        }


        /// <summary>
        /// PostEventRules will post to another event the rules it need to adopt
        /// </summary>
        /// <param name="rules">The rule-set it need to adopt</param>
        /// <param name="ownId">The id of the calling event</param>
        public async Task PostEventRules(EventRuleDto rules, string ownId)
        {
            await _httpClient.Create(String.Format("event/rules/{0}", ownId), rules);
        }

        /// <summary>
        /// UpdateEventRules will post to another Event the rules it need to update.
        /// </summary>
        /// <param name="replacingRules">The new (replacing ruleset)</param>
        /// <param name="ownId">The id of the calling event</param>
        // TODO: Will the replacing ruleset contain all rules from this Event (whether or not they all need to be updated) or only those that need to be modified? 
        // The definition of the PUT-call is that the state will be set to whatever is received, so every rule
        // must be defined in replacingRules. - Mikael.
        public async Task UpdateEventRules(EventRuleDto replacingRules, string ownId)
        {
            await _httpClient.Update(String.Format("event/rules/{0}", ownId), replacingRules);
        }

        /// <summary>
        /// Will issue a Delete call on receiving Event's rules. How the receiving Event handles this call
        /// is an implementation detail 
        /// </summary>
        /// <param name="ownId">The id of the calling event</param>
        public async Task DeleteEventRules(string ownId)
        {
            await _httpClient.Delete(String.Format("event/rules/{0}", ownId));
        }

        public async Task SendNotify(IEnumerable<NotifyDto> dtos)
        {
            await _httpClient.Update("event/notify", dtos);
        }
    }
}