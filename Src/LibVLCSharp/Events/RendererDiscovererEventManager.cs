﻿using System;
using LibVLCSharp.Shared.Helpers;

namespace LibVLCSharp.Shared
{
    internal class RendererDiscovererEventManager : EventManager
    {
        readonly object _lock = new object();

        EventHandler<RendererDiscovererItemAddedEventArgs> _itemAdded;
        EventHandler<RendererDiscovererItemDeletedEventArgs> _itemDeleted;

        int _itemAddedRegistrationCount;
        int _itemDeletedRegistrationCount;

        EventCallback _itemAddedCallback;
        EventCallback _itemDeletedCallback;

        internal RendererDiscovererEventManager(IntPtr ptr) : base(ptr)
        {
        }

        protected internal override void AttachEvent<T>(EventType eventType, EventHandler<T> eventHandler)
        {
            lock (_lock)
            {
                switch(eventType)
                {
                    case EventType.RendererDiscovererItemAdded:
                        Attach(eventType,
                            ref _itemAddedRegistrationCount,
                            () => _itemAdded += eventHandler as EventHandler<RendererDiscovererItemAddedEventArgs>,
                            () => _itemAddedCallback = OnItemAdded);
                        break;
                    case EventType.RendererDiscovererItemDeleted:
                        Attach(eventType,
                            ref _itemDeletedRegistrationCount,
                            () => _itemDeleted += eventHandler as EventHandler<RendererDiscovererItemDeletedEventArgs>,
                            () => _itemDeletedCallback = OnItemDeleted);
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
                    case EventType.RendererDiscovererItemAdded:
                        Detach(eventType,
                            ref _itemAddedRegistrationCount,
                            () => _itemAdded -= eventHandler as EventHandler<RendererDiscovererItemAddedEventArgs>,
                            ref _itemAddedCallback);
                        break;
                    case EventType.RendererDiscovererItemDeleted:
                        Detach(eventType,
                            ref _itemDeletedRegistrationCount,
                            () => _itemDeleted -= eventHandler as EventHandler<RendererDiscovererItemDeletedEventArgs>,
                            ref _itemDeletedCallback);
                        break;
                    default:
                        OnEventUnhandled(this, eventType);
                        break;
                }
            }
        }


        void OnItemDeleted(IntPtr args)
        {
            var rendererItem = RetrieveEvent(args).Union.RendererDiscovererItemDeleted;
            _itemDeleted?.Invoke(this, new RendererDiscovererItemDeletedEventArgs(new RendererItem(rendererItem.item)));
        }

        void OnItemAdded(IntPtr args)
        {
            var rendererItem = RetrieveEvent(args).Union.RendererDiscovererItemAdded;           
            _itemAdded?.Invoke(this, new RendererDiscovererItemAddedEventArgs(new RendererItem(rendererItem.item)));
        }
    }
}
