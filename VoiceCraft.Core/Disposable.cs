/////////////////////////////////////////////////////////////////////////////////
// paint.net                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, and contributors.                  //
// All Rights Reserved.                                                        //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace VoiceCraft.Core
{
    /// <summary>
    /// Provides a standard implementation of IDisposable and IIsDisposed.
    /// </summary>
    [Serializable]
    public abstract class Disposable : IDisposable
    {
        private int isDisposed; // 0 for false, 1 for true

        public bool IsDisposed
        {
            get => Volatile.Read(ref this.isDisposed) != 0;
        }

        protected Disposable()
        {
        }

        ~Disposable()
        {
            int oldIsDisposed = Interlocked.Exchange(ref this.isDisposed, 1);
            if (oldIsDisposed == 0)
            {
                Dispose(false);
            }
        }

        public void Dispose()
        {
            int oldIsDisposed = Interlocked.Exchange(ref this.isDisposed, 1);
            if (oldIsDisposed == 0)
            {
                try
                {
                    Dispose(true);
                }
                finally
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}

// https://gist.github.com/rickbrew/fc3e660c0930747f031e64ab7696c60d