﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSObserver.Models
{
    interface IFileChange
    {
        void loadXMLRule(string path);
    }
}
