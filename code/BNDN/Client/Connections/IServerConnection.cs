﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.History;

namespace Client.Connections
{
    public interface IServerConnection : IDisposable
    {
        Task<RolesOnWorkflowsDto> Login(string username);
        Task<IList<WorkflowDto>> GetWorkflows();
        Task<IList<EventAddressDto>> GetEventsFromWorkflow(WorkflowDto workflow);
        
        /// <summary>
        /// Returns the history of the event.
        /// </summary>
        /// <returns></returns>
        Task<IList<HistoryDto>> GetHistory(string workflowId);
    }
}
