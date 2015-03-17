﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Http;
using Common;

namespace Event.Controllers
{
    [RoutePrefix("event")]
    public class EventController : ApiController
    {
        public State State { get; set; }
        public EventController()
        {
            State = State.GetState();
        }

        #region EventEvent
        [HttpGet]
        public async Task<EventDto> Get()
        {
            return await State.EventDto;
        }

        #region Rules
        [Route("rules/{id}")]
        [HttpPost]
        public async Task<IHttpActionResult> PostRules(string id, [FromBody] EventRuleDto ruleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (ruleDto == null)
            {
                return BadRequest("Request requires data");
            }

            // if new entry, add Event to the endPoints-table.
            if (State.KnowsId(id))
            {
                return BadRequest(string.Format("{0} already exists!", id));
            }
            var addresses = (await Dns.GetHostAddressesAsync(Request.RequestUri.Host)).Where(address => address.AddressFamily == AddressFamily.InterNetwork).ToArray();
            if (!addresses.Any() || addresses.Length > 1)
            {
                throw new Exception("Bad address!" + addresses.Length);
            }

            

            var endPoint = new IPEndPoint(addresses[0], Request.RequestUri.Port);
            State.RegisterIdWithEndPoint(id, endPoint);

            // Todo: Refactor code below:
            // Condition
            if (ruleDto.Condition)
            {
                State.Conditions.Add(endPoint);
            }
            else
            {
                State.Conditions.Remove(endPoint);
            }

            // Exclusion
            if (ruleDto.Exclusion)
            {
                State.Exclusions.Add(endPoint);
            }
            else
            {
                State.Exclusions.Remove(endPoint);
            }

            // Inclusion
            if (ruleDto.Inclusion)
            {
                State.Inclusions.Add(endPoint);
            }
            else
            {
                State.Inclusions.Remove(endPoint);
            }

            // Response
            if (ruleDto.Response)
            {
                State.Responses.Add(endPoint);
            }
            else
            {
                State.Responses.Remove(endPoint);
            }
            return Ok();
        }

        [Route("rules/{id}")]
        [HttpPut]
        public async Task<IHttpActionResult> PutRules(string id, [FromBody] EventRuleDto ruleDto)
        {
            // if new entry, add Event to the endPoints-table.
            if (!State.KnowsId(id))
            {
                return BadRequest(string.Format("{0} does not exist!", id));
            }

            var endPoint = State.GetEndPointFromId(id);

            // Todo: Refactor code below:

            // Condition
            if (ruleDto.Condition)
            {
                State.Conditions.Add(endPoint);
            }
            else
            {
                State.Conditions.Remove(endPoint);
            }

            // Exclusion
            if (ruleDto.Exclusion)
            {
                State.Exclusions.Add(endPoint);
            }
            else
            {
                State.Exclusions.Remove(endPoint);
            }

            // Inclusion
            if (ruleDto.Inclusion)
            {
                State.Inclusions.Add(endPoint);
            }
            else
            {
                State.Inclusions.Remove(endPoint);
            }

            // Response
            if (ruleDto.Response)
            {
                State.Responses.Add(endPoint);
            }
            else
            {
                State.Responses.Remove(endPoint);
            }
            return Ok();
        }

        [Route("rules/{id}")]
        [HttpDelete]
        public async Task<IHttpActionResult> DeleteRules(string id)
        {
            if (!State.KnowsId(id))
            {
                return BadRequest(string.Format("{0} does not exist!", id));
            }
            var endPoint = State.GetEndPointFromId(id);
            await Task.Run(() =>
            {
                // Todo: Locking or running all 4 statements concurrently.
                State.Conditions.RemoveWhere(end => end.Equals(endPoint));
                State.Exclusions.RemoveWhere(end => end.Equals(endPoint));
                State.Inclusions.RemoveWhere(end => end.Equals(endPoint));
                State.Responses.RemoveWhere(end => end.Equals(endPoint));
                State.RemoveIdAndEndPoint(id);
            });
            return Ok();
        }
        #endregion
        #endregion

        #region ClientEvent

        [Route("state")]
        [HttpGet]
        public async Task<EventStateDto> GetState()
        {
            return await State.EventStateDto;
        }

        [HttpPut]
        public async Task<IHttpActionResult> Execute(bool execute)
        {
            if (execute)
            {
                if ((await (State.EventStateDto)).Executable)
                {
                    State.Executed = true;
                    return BadRequest();
                }
                return BadRequest("Not possible to execute event.");
            }
            else
            {
                // Todo: Is this what should happen when execute is false?
                State.Executed = false;
                return Ok();
            }
        }
        #endregion
    }
}