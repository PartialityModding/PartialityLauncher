First, before I say anything, I need to cover something.

There's two kinds of mods supported by Partiality. 

**1:DLL Mods, or "External Mods".** These mods are self-contained .dll's that reference the original game's code/the Partiality API.

**2:Patch mods.** These mods are much different. Patch mods are inserted to the game's code through the launcher. Because of this, they can change exisitng game code in any way, shape, and form.

Now that that's out of the way...

__***Making DLL Mods***__

Making DLL mods is pretty simple, most of your time is going to be spent learning the game itself. If you want to, say, add a new item to a game, you'll first have to figure out how the game manages items, and how you can add to that system.

To set up a project for a DLL mod, all you need to do is make a C# project using your favorite IDE (I recommend Visual Studio). Set the build type to be a .NET Class Library (.dll file).
Once you've done that, add the Partiality.dll, UnityEngine.dll, and Assembly-CSharp.dll to your project's references. The Partiality.dll is avaliable on the Github (https://github.com/PartialityModding/Partiality/releases ). UnityEngine.dll and Assembly-CSharp.dll are in [Path to game]/[GameName]_Data/Managed. I'd make backups of these both.

After that, make a new C# class, and have it inherit from Partiality.ModLoader.PartialityMod. By doing so, you tell Partiality to call some functions and the like on that class.
Init() is called the moment the mod is loaded. Use this to set properties or load configs, and the like.
OnLoad() is called after all mods have loaded. Do most of your first-time code here. Creating objects, getting stuff from other mods, etc.
OnEnable() is called when a mod is enabled (also when it's loaded)
OnDisable() is called when a mod is disabled.

All you need to do is override one of the functions and it'll get called when needed.

__***Making Patch Mods***__

Making Patch Mods is much trickier than DLL mods. Get ready to crash the game a lot, and have to re-download or restore it many, many more times.

First, you'll start by making a C# project, just like with a .DLL mod. Use any IDE you like. Set the build type to .NET Class Library (.dll file). Add UnityEngine.dll, and Assembly-CSharp.dll to your references for the project. Same with Partiality.dll, if you need it for something. Next, I recommend  you add MonoMod to your references. Some stuff is a lot harder to do without it. You can get MonoMod here (https://github.com/0x0ade/MonoMod). Build it, and just add the .exe to your references.

Once you're all set up, you can start making your patch. Before we move forward, I want to make a note. ***Patches will likely never support stacking over each other.*** If a patch modifies a function, then ANY other patch that ALSO modifies that function won't be compatible with it, because as far as I'm aware, if you ***patch over a patched function, you'll delete the original, or it will ignore it.*** Either way, the result won't be useful to anyone. Because of this, I HIGHLY recommend  AGAINST patching the game unless it's ABSOLUTELY required.

Alright, onto the actual patching.
First, let's take a look at an example class:


```cs
    public class ExampleClass {
        private int privateVariable = 100;

        public void PublicFunction() {
            privateVariable++;
        }

        private void PrivateFunction() {
            privateVariable--;
        }
    }
```

For the most part, this script is simple. We have a private int (privateVariable) which is equal to 100. 
We also have the functions PublicFunction() which increases privateVariable by 1, and PrivateFunction, which decreases privateVariable by 1.

So, how do we patch this class?

Well, all we have to do is make a class and give it the right attributes so that MonoMod knows to patch it.
Let's look at the patch.

```cs
    [MonoMod.MonoModPatch("global::ExampleClass")]
    public class patch_ExampleClass {

        [MonoMod.MonoModIgnore]
        private int privateVariable;

        public extern void orig_PublicFunction();
        public void PublicFunction() {
            //Run our own code either before
            orig_PublicFunction();
            //Or after the original function is called.
        }

        public extern void orig_PrivateFunction();
        public void PrivateFunction() {
            orig_PrivateFunction();
        }

    }
```

Now, that's not that different from the original class. There's only a few differences.
Let's go line-by-line.
```cs
    [MonoMod.MonoModPatch("global::ExampleClass")]
```
This one's pretty simple. Basically, this attribute is telling MonoMod that this class (patch_ExampleClass) is a patch for the class global::ExampleClass. When you're writing this out, make sure you put the full "path" to the class you're patching. This includes any namespaces (global::ExampleNamespace.ExampleClass).

```cs
    public class patch_ExampleClass
```
This one's easy too. Basically we're just making a new class called patch_ExampleClass. It doesn't HAVE to be called this, you can call it whatever you want. I just use it because it's explicit and makes sense.

```cs
        [MonoMod.MonoModIgnore]
        private int privateVariable;
```
This is interesting. Why are we re-declaring the same variable as before? Well, the attribute should hint a bit towards what's going on. MonoModIgnore basically tells the patcher "Completely ignore this thing." It prevents it from being added to the patched file. Now, some of the more expirienced programmers may ask "But won't that cause all kinds of errors and bad things? You're referencing a variable that doesn't exist because it won't be added to the patched class.
The thing is, because of how C#/The patcher is written, the fact that they are EXACTLY the same name means that, when the patcher is done, any references to the privateVariable in the patched class ***will be*** references to the ***original*** privateVariable. This means that we can access any variable in the original class, even if it's private.

Next!
```cs
  public extern void orig_PublicFunction();
```
This one's pretty simple. Basically, by prefixing the function with "orig_" (also, there's an attribute that does this, if you prefer it without the prefix), we're telling the patcher to replace this with the origina PublicFunction. So, all the code that's in PublicFunction() in the original ExampleClass will be put into orig_PublicFunction().

```cs
public void PublicFunction() {
            //Run our own code either before
            orig_PublicFunction();
            //Or after the original function is called.
        }
```
With the last one explained, this one's pretty easy to get. This is the new PublicFunction. Any time code calls PublicFunction in ExampleClass, it will run this code. In this example, I called the original code in the middle of two comments. You can put ANY code you want before or after the original function call. You can also just not call the orignal function at all, if you like. That can really screw stuff up if you're not careful though.

```cs
        public extern void orig_PrivateFunction();
        public void PrivateFunction() {
            orig_PrivateFunction();
        }
```
Last, I just did the same thing with PrivateFunction, to show that it works with, well. Private functions.

There's a lot more you can do with patching, but it just comes from expirimenting and looking through all the MonoMod attributes.
