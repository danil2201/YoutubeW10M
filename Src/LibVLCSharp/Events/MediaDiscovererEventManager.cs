﻿using System;
using LibVLCSharp.Shared.Helpers;

namespace LibVLCSharp.Shared
{
    internal class MediaDiscovererEventManager : EventManager
    {
        readonly object _lock = new object();

        EventHandler<EventArgs> _mediaDiscovererStarted;
        EventHandler<EventArgs> _mediaDiscovererStopped;

        int _discovererStartedRegistrationCount;
        int _discovererStoppedRegistrationCount;

        EventCallback _discovererStartedCallback;
        EventCallback _discovererStoppedCallback;

        public MediaDiscovererEventManager(IntPtr ptr) : base(ptr)
        {
        }

        protected internal override void AttachEvent<T>(EventType eventType, EventHandler<T> eventHandler)
        {
            lock (_lock)
            {
                switch (eventType)
                {
                    case EventType.MediaDiscovererStarted:
                        Attach(eventType,
                            ref _discovererStartedRegistrationCount,
                            () => _mediaDiscovererStarted += eventHandler as EventHandler<EventArgs>,
                            () => _discovererStartedCallback = OnStarted);
                        break;
                    case EventType.MediaDiscovererStopped:
                        Attach(eventType,
                            ref _discovererStoppedRegistrationCount,
                            () => _mediaDiscovererStopped += eventHandler as EventHandler<EventArgs>,
                            () => _discovererStoppedCallback = OnStopped);
                        break;
                    default:
                        OnEventUnhandled(this, eventType);
                        break;
                }
            }
        }

        protected internal override void DetachEvent<T>(EventType eventType, EventHandler<T> eventHandler)
        {
            lock (_lock)
            {
                switch (eventType)
                {
                    case EventType.MediaDiscovererStarted:
                        Detach(eventType,
                            ref _discovererStartedRegistrationCount,
                            () => _mediaDiscovererStarted -= eventHandler as EventHandler<EventArgs>,
                            ref _discovererStartedCallback);
                        break;
                    case EventType.MediaDiscovererStopped:
                        Detach(eventType,
                            ref _discovererStoppedRegistrationCount,
                            () => _mediaDiscovererStopped -= eventHandler as EventHandler<EventArgs>,
                            ref _discovererStoppedCallback);
                        break;
                    default:
                        OnEventUnhandled(this, eventType);
                        break;
                }
            }
        }

        void OnStarted(IntPtr ptr)
        {
            _mediaDiscovererStarted?.Invoke(this, EventArgs.Empty);
        }

        void OnStopped(IntPtr ptr)
        {
            _mediaDiscovererStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
