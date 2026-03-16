using System;
using System.Collections.Generic;


/// <summary>
/// Class static représentant un eventbus générique
/// 
/// Comment l'utiliser : 
///     I :
///     Créer une struct qui contient contient les données à transmettre
///         public struct ReceiveMessageEvent
///         {
///            public MessageStruct Message;
///         }
///         
///     II : 
///     S'abonner à l'event : 
///         private void OnEnable()
///         {
///             EventBus.Subscribe<ReceiveMessageEvent>(OnMessageReceived);
///         }
///         
///         private void OnDisable()
///         {
///             EventBus.Unsubscribe<ReceiveMessageEvent>(OnMessageReceived);
///         }
/// 
///     III : 
///     Publish l'event
///     EventBus.Publish(new ReceiveMessageEvent
///     {
///         Message = messageStruct
///     });
///     
///     IV :
///     Recevoir l’Event
///     private void OnMessageReceived(ReceiveMessageEvent evt)
///     {
///         CreateNewMessage().SetUpMessage(evt.Message);
///     }
/// 
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _events = new();

    public static void Subscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);

        if (_events.TryGetValue(type, out Delegate existing))
        {
            _events[type] = Delegate.Combine(existing, listener);
        }
        else
        {
            _events[type] = listener;
        }
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);

        if (!_events.TryGetValue(type, out Delegate existing))
            return;

        Delegate currentDel = Delegate.Remove(existing, listener);

        if (currentDel == null)
        {
            _events.Remove(type);
        }
        else
        {
            _events[type] = currentDel;
        }    
    }

    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);

        if (_events.TryGetValue(type, out Delegate del))
        {
            ((Action<T>)del)?.Invoke(eventData);
        }
    }
}
