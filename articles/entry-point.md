# Entry Point and Building your App
If you plan on making anything useful, it's not enough to simply create a new Console app that contains a reference to Thundershock Engine - for obvious reasons. Before you can start working with Thundershock itself, you'll need to initialize it in your application. The ``EntryPoint.Run`` method in ``Thundershock.Core`` aims to help get the engine loaded for you, and starts running an [App](app-types.md) of your choice.

```cs
static void Main(string[] args)
{
    EntryPoint.Run<T>(args);
}
```

where ``T`` is intended to be a class inheriting from the ``Thundershock.NewGameAppBase`` or ``Thundershock.GraphicalAppBase`` classes.

For all intents and purposes, you should use ``NewGameAppBase`` for game development. ``GraphicalAppBase`` is intended for making tools and other utilities where you ultimately want to add other UI elements to your application (imagine a level editior or similar). [Learn more about app types here](app-types.md).

**Moving forward, we'll be using ``NewGameAppBase`` in this example.**

## Create Your App
Before we continue, it's important to realize where in your game/application different operations are taking place. If we took the order in which objects are created in Thundershock and turned them into a tree, it would look something like this;

```
 - GameWindow
  - App
   - LayerManager
    - GameLayer
     - The current Scene
       - Post-processor
       - Scene GUI
       - Entity Component System
    - FPS counter for debug builds
    - Tilde debug console
```

The ``GameWindow`` is created first, followed by the ``App`` as requested in our ``EntryPoint.Run<T>()`` call (where we are right now). As you can see, however, there's not much that we can actually do within an ``App`` without going further down the tree. In order to interact with the [Entity Component System](ecs.md) for example, we have a few prerequisites;

- ``LayerManager``
- ``GameLayer``
- The current ``Scene``

Thankfully, Thundershock was kind enough to create ``LayerManager`` and its child, ``GameLayer`` on its own - leaving the task of loading a new ``Scene`` up to us.

Creating the Class for our App is very simple;

```cs
using Thundershock;

// ...

class MyApp : NewGameAppBase
{

}
```

Pass the name of your new App Class into the ``<T>`` parameter of the ``EntryPoint.Run<T>()`` method. *Again, this should be in the ``Main(string[] args)`` method within ``Program.cs``.*

You've now created a blank app, and you can load it and startup. Behind the scenes, this is doing *a lot* of work - namely, starting up Thundershock Engine!

## Scenes
Think of a Scene as a place where 'your game' happens. A Scene is a level, place, or stage in your game - your terrain, characters, objects, and their behaviors live here. We'll be digging further into creating Scenes soon - and actually adding some functionality to your project.

For now, let's create a Scene to load from our App. Make a new Class in your game's project, which, much like creating your App, is a very simple task;

```cs
using Thundershock;

// ...

class MyScene : Scene
{

}
```

Move back to your App class. With the ``Scene`` created, we can load it from the App. This is as simple as overriding ``NewGameAppBase``'s ``OnLoad()`` method, and calling ``Thundershock.LoadScene<T>()`` like so;

```cs
// inside the App class
override protected void OnLoad()
{
    LoadScene<T>();
}
```

where ``<T>`` is the Class you made which inherits from ``Scene``.

Launch the game. Congratulations! You started Thundershock Engine. Move on to [building your first scene](hello-scene.md).
