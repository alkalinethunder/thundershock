# Getting Started

If you're reading this, you've probably decided that you're interested in learning how to set up and use Thundershock Engine in your own project. This article will show you how to do it. Please note however that the engine is extremely early in development and thus both lacks a lot of features and isn't friendly to work with at times. Please don't expect to have a fully working game in this engine unless you really know what you're doing or are prepared to start modifying the engine.

## This is an engine, not a framework!

It's important to understand the difference between a Game Engine and a Game Framework. In fact, a Game Framework is a **component** of a game engine. An engine's job is to make it easier for you to develop a game and get it working, and to hide away most of the heavy lifting that goes with components like rendering, input and audio.

IF YOU WANT TO DO LOW-LEVEL HEAVY LIFTING STUFF, please go to [MonoGame](https://monogame.net) or something similar - Thundershock, although originally built using MonoGame as a foundation and having some similar APIs, is not MonoGame.

## What is Thundershock/Why does it exist?

Thundershock is a 3D, open-source, OpenGL-based, cross-platform game engine written in C# and .NET 5. It exists because I wanted a tool that would make it easy for me to build my games and applications. While I could have used something like Unity or Unreal Engine, I find those engines to be far more heavy than what I need. Which leaves me to using things like MonoGame since my favourite language happens to be C#. However I find myself fighting those APIs a lot more than being helped out behind them. I wanted to roll my own engine as a personal challenge, as a learning experience, so I could understand the work that goes into MonoGame and other tools, but so I could also design the API in a way that makes logical sense to me. In essence, Thundershock is my passion project and it exists because I want it to.

## Enough blubbering on. How do you use it?

I want to eventually set up some sort of NuGet package that you can just install into your IDE, maybe some project templates. Hell, maybe even a graphical level editor. I just haven't gotten nearly that far. So instead I'll show you how I get the engine running with my own projects in such a way that I can develop the engine alongside them. In fact, I'm writing this inside the Socially Distant repository - and not directly inside Thundershock's.

Start by creating a new Git repository. I prefer [GitHub](https://github.com) because it's free, and you can create private projects for free now. Thundershock's license allows you to do that provided you give me credit for the engine or any code you pull from it.

In the following code samples I'll be using https://github.com/redteam-os/redteam-frontend as an **example** repository - replace it with your own.

### Initialize your repository locally

First we need to initialize a local Git repository. You **have to do this**, and you should even just as a good development practice. You have to, because Thundershock is pulled in as a Git submodule.

```bash
# make a folder for our game
mkdir my-game
cd my-game

# create a local git repo
git init

# set up the origin repository, where will we push code up to?
git remote add origin https://github.com/redteam-os/redteam-frontend

# create a Visual Studio gitignore using dotnet
dotnet new gitignore

# Add, commit, push.
git add .gitignore
git commit -m "Initial commit"
git push -u origin master
```

### Pulling Thundershock into the repository

We're now going to pull Thundershock in as a submodule. This script should be executed in your local git repository (inside `my-game` above). This will pull in the `master` branch of Thundershock, which in most cases, should always be the branch you deal with.

```bash
# clone thundershock into this repository as a submodules
git submodule add https://github.com/redteam-os/thundershock

# Add it as if it were a file
git add thundershock

# Don't forget .gitmodules!!!
git add .gitmodules

# Commit these changes.
git commit -m "A distant thunder strike occurs..."
git push
```

Now, your repository should look like this:

```
my-game/
    .git/
    thundershock/
    .gitignore
    .gitmodules
```

### Creating the .NET Project

Now let's create our .NET project. I would recommend creating the projects through command-line, then using an IDE to add project references.

```bash
# Make a directory for our game projects
mkdir src
cd src

# Create a Visual Studio/Rider solution.
dotnet new sln

# Rename the solution
mv src.sln MyGame.sln

# Create a directory for our game code.
mkdir MyGame
cd MyGame

# Create a console app.
dotnet new console

# Go up a directory.
cd ..

# Add our game project, followed by Thundershock's projects, to the solution.
dotnet sln add MyGame                                   # our game
dotnet sln add ..\thundershock\src\Thundershock         # thundershock's game framework
dotnet sln add ..\thundershock\src\Thundershock.OpenGL  # OpenGL (rendering), OpenAL (audio), and SDL2 (windowing and input) implementation of Thundershock.
dotnet sln add ..\thundershock\src\Thundershock.Core    # thundershock's core APIs.
```

Now, open your solution in your preferred IDE. From here, you will need to do the following:

1. Create two Solution Folders: "Engine" and "Game". Move all `Thundershock`-related projects to "Engine" and move `MyGame` to "Game". Collapse the "Engine" one and never look at it again unless you have to. Seriously, you'll thank me later.
2. Add a Project Reference to `MyGame`, make sure it references `Thundershock`. The other two projects will be automatically referenced by `Thundershock`.
3. You're done! Commit.

```bash
# in repository root
git add *
git commit -m "Set up initial project"
git push
```

Any time you need to clone your repository, make sure to use the `--recurse-submodules` flag to tell Git to pull in Thundershock automatically:

```bash
git clone --recurse-submodules https://github.com/redteam-os/redteam-frontend
```

If I add new features to the engine and you're feeling bold, do this to get the updated code:

```bash
# in repository toot
cd thundershock
git pull origin HEAD:master
cd ..

# AFTER TESTING THINGS OUT!!! I like breaking old code.
git add thundershock
git commit -m "Update Thundershock"
git push
```

## You're all set!

At this point you now have an empty Thundershock project that just spits out Hello World to the console and doesn't actually initialize Thundershock. From here, the next step is to learn how to [build an application](entry-point.md).