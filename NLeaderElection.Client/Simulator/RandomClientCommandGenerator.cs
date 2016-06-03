using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NLeaderElection.Client.Simulator
{
    internal class RandomClientCommandGenerator : IDisposable
    {

        private static Timer randomCommandEmitter;
        
        public RandomClientCommandGenerator()
        {
            randomCommandEmitter = new Timer(5000);
        }

        public void Simulate()
        {
            randomCommandEmitter.Start();
            randomCommandEmitter.Elapsed += randomCommandEmitter_Elapsed;
            Logger.Log("INFO :: Client command simulator started.");
        }

        static void randomCommandEmitter_Elapsed(object sender, ElapsedEventArgs e)
        {
            string commandText = "commandText" + DateTime.Now.ToShortTimeString();
            var leader = NodeRegistryCache.GetInstance().CurrentNode as Leader;
            if (leader != null)
            {
                leader.ExecuteCommand(commandText);
            }
            else
            {
                Logger.Log("Warning ! Client command simulator generated a new command. Local node is not a leader.");
            }
        }

        public static void Stop()
        {
            randomCommandEmitter.Elapsed -= randomCommandEmitter_Elapsed;
            randomCommandEmitter.Stop();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (randomCommandEmitter != null) randomCommandEmitter.Dispose();
            }
        }

        ~RandomClientCommandGenerator()
        {
            Dispose(false);
        }
    }
}
