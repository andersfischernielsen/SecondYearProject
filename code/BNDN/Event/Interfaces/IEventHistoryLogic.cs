﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.History;

namespace Event.Interfaces
{
    public interface IEventHistoryLogic : IDisposable
    {
        Task SaveHistory(HistoryModel toSave);

        Task<IEnumerable<HistoryDto>> GetHistoryForEvent(string workflowId, string eventId);

        Task SaveException(Exception ex, string requestType, string method, string eventId = "", string workflowId = "");

        Task SaveSuccesfullCall(string requestType, string method, string eventId = "", string workflowId = "");
    }
}