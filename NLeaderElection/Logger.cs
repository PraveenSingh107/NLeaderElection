using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NLeaderElection
{
    public static class Logger
    {
        
        public static void Log(Exception exp)
        {
            do
            {
                // TO DO Wrtie log using log4net
                Console.WriteLine(string.Format("Stack trace : {0}. Message : {1}", exp.StackTrace, exp.Message));
                exp = exp.InnerException;
            }
            while (exp != null);
        }

        public static void Log(string msg)
        {
            Console.WriteLine(String.Format("{0} : {1}",Thread.CurrentThread.ManagedThreadId, msg));
           // TO DO
        }
    }
}
