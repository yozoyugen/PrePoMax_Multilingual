﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using CaeGlobals;
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    public abstract class Command   //: ISerializable - this would mean that all derived classes must be Serializable !!!
    {
        // Variables                                                                                                                
        protected string _name;
        protected DateTime _dateCreated;
        protected TimeSpan _timeSpan;

        // Properties                                                                                                               
        public string Name { get { return _name; } }
        public TimeSpan TimeSpan { get { return _timeSpan; } set { _timeSpan = value; } }


        // Constructors                                                                                                             
        public Command(string name)
        {
            _name = name;
            _dateCreated = DateTime.Now;
        }


        // Methods                                                                                                                  
        public virtual bool Execute(Controller receiver)
        {
            return true;
        }
        public string GetDateTime()
        {
            return _dateCreated.ToString("MM/dd/yy HH:mm:ss");
        }
        public virtual string GetCommandString()
        {
            return GetBaseCommandString();
        }
        public string GetBaseCommandString()
        {
            return _dateCreated.ToString("MM/dd/yy HH:mm:ss") + "   " + _name + ": ";
        }
        protected string GetArrayAsString(string[] array)
        {
            string names = "[";
            int count = 0;
            int maxLen = 120;
            foreach (string name in array)
            {
                names += name;
                if (++count < array.Length) names += ", ";
                if (names.Length > maxLen)
                {
                    names += "...";
                    break;
                }
            }
            names += "]";

            return names;
        }
    }
}
