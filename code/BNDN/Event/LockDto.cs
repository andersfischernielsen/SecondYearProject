﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace Event
{
    public class LockDto
    {
        //It's expected that LockOwner matches the Id of the EventAddressDto making the lock call.
        public string LockOwner { get; set; }
        // Used for database purposes
        public int Id { get; set; }
    }
}