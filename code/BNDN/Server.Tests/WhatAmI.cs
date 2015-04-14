﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Server.Models;
using Server.Storage;

namespace Server.Tests
{
    [TestFixture]
    class WhatAmI
    {
        private ServerStorage _s;

        [Test]
        public async void Test1()
        {
            _s = new ServerStorage();
            var wm = new ServerWorkflowModel
            {
                Name = "Test2",
                Id = "1",
                ServerEventModels = new List<ServerEventModel>(),
                ServerRolesModels = new List<ServerRoleModel>()
            };
            await _s.AddNewWorkflow(wm);
        }

        [Test]
        public async void Test2()
        {
            _s = new ServerStorage();
            var v = _s.GetWorkflow("1");
            await _s.RemoveWorkflow(v);
        }

        [Test]
        public async void Test3()
        {
            _s = new ServerStorage();
            var wm = new ServerWorkflowModel
            {
                Name = "Test2",
                Id = "1",
                ServerEventModels = new List<ServerEventModel>(),
                ServerRolesModels = new List<ServerRoleModel>()
            };
            await _s.AddNewWorkflow(wm);

            var e = new ServerEventModel()
            {
                Id = "Adam",
                ServerWorkflowModelId = "1",
                Uri = "http://www.google.dk",
                ServerWorkflowModel = _s.GetWorkflow("1")
            };

            await _s.AddEventToWorkflow(e);
        }

        [Test]
        public void Test4()
        {
            _s = new ServerStorage();
            _s.RemoveEventFromWorkflow(_s.GetWorkflow("1"), "Adam");
        }
    }
}
