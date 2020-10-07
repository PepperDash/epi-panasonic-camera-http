using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PanasonicCameraEpi
{
    public abstract class CommandQueue : IDisposable, IKeyed
    {
        protected readonly CrestronQueue<string> _cmdQueue;
        private readonly Thread _worker;
        protected readonly CEvent _wh = new CEvent();

        public string Key { get; private set; }

        public bool Disposed { get; private set; }

        protected CommandQueue(IBasicCommunication coms)
        {
            _cmdQueue = new CrestronQueue<string>();
            _worker = new Thread(ProcessCmd, coms, Thread.eThreadStartOptions.Running);
            _worker.Name = coms.Key + "-Thread";
            Key = coms.Key;

            CrestronEnvironment.ProgramStatusEventHandler += programEvent =>
            {
                if (programEvent != eProgramStatusEventType.Stopping)
                    return;

                Debug.Console(1, coms, "Shutting down the coms processor...");
                Dispose();
            };
        }

        protected abstract object ProcessCmd(object obj);

        public void EnqueueCmd(string cmd)
        {
            _cmdQueue.Enqueue(cmd);
            _wh.Set();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            CrestronEnvironment.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                EnqueueCmd(null);
                _worker.Join();
                _wh.Close();
            }

            Disposed = true;
        }

        ~CommandQueue()
        {
            Dispose(false);
        }

        #endregion
    }

}