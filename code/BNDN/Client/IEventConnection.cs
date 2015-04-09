﻿using System.Threading.Tasks;
using Common;

namespace Client
{
    /// <summary>
    /// Connection to an event
    /// </summary>
    public interface IEventConnection
    {
        /// <summary>
        /// Get the state of a task
        /// </summary>
        /// <returns></returns>
        Task<EventStateDto> GetState();

        /// <summary>
        /// Delete an event. Only to be used for testing!
        /// TODO: REMOVE THIS METHOD WHEN DONE WITH TESTING.
        /// </summary>
        /// <returns></returns>
        Task DeleteEvent();
        
        /// <summary>
        /// Execute a task
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        Task Execute(bool b, string workflowId);
    }
}
