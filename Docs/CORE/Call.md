## Call
- Reference: [See here]()

### Usage
- This feature is the bridge from internal to external events, that is, any action that is invoked when some event passes through it, and it allows us to do something like create a thread that will just dispatch or publish the events, this applies very well in game engines, where an external thread is not allowed to manipulate the engine state, here is an example if you get a message to change the color of a ui text and you change it inside the event callback, this change does not will take effect, ie. the engine will ignore, or prevent this thread from handling the components, and to solve this we usually create a script that runs using the engine's main thread and all events we receive we add to the event list and the main thread will dispatch and with that, the event will be executed using the main thread, and with that our text color change example would work because the thread that executed the event is main. and with this feature we only need to make two changes so that you customize the thread that will dispatch or publish the event: 1. Add it as manual, so that messages are not automatically dispatched with the thread that created it, 2. Call a function that dispatch, as the state has been added as manual, so no events will be dispatched or published. So we need to call a method that every time we call it it publishes the events, here is the magic, the thread that calls this event becomes its owner, that is, if we create a Thread that only dispatches events then all the events will belong to it, and with that in the game engine example and just create a script that is executed with the main thread that is executing this method, and all the events will belong to the main thread with this inside the event callback we can manipulate the engine state as change the color of a text.

### Example
  - Dispatch or automatic publication, by default the thread that dispatches the event is the same that creates it, we can see how to do it
    ```csharp
    using Netly.Core;
    
    public class Example
    {
        public void Init()
        {
            /* 
              true: All events are dispatched or published by the thread that created them.
              false: Events will not be published or dispatched automatically.
            */
            Call.Automatic = true;
        }
    }
    ```

  - Manual Publisher or Dispatcher, That way we can specify a thread to publish or dispatch the events.
    - This example with Unity Engine
    ```csharp
    public class Example : MonoBehaviour
    {
        // Called on startup
        public void Start()
        {
            /* 
              true: All events are dispatched or published by the thread that created them.
              false: Events will not be published or dispatched automatically.
            */
            Call.Automatic = false;
        }
        
        // Called on every frame
        public void Update()
        {        
            // Here we are publishing or dispatching events in the main thread script loop
            Call.Publish();            
        }
    }
    ```
  - Add an event, to be dispatched
    ```csharp
    using System;
    using Netly.Core;

    public class Example
    {
        public void Init()
        {
            // Adding event to be dispatched.
            Call.Execute(MyEvent);
        }
        
        private void MyEvent()
        {
            Console.WriteLine("My event 1");
            
            // Adding event to be dispatched. Using lambda or anonymous function
            Call.Execute(() =>
            {
                Console.WriteLine("My event 2");              
            });
        }
    }
    ```
