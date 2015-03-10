﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Models
{
    public class Workflow
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<Activity> Activities { get; set; }
    }
}