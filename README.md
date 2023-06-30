# OKTO-TechTest
Technical test for OKTO job application

## Objective
"Implement swipe functionality using Unity UI Toolkit to create a TikTok-like feed
where users can swipe up and down between "dance challenges" content. Select characters
from the asset store or Mixamo and use different dancing animation clips for each content
slide. The demo should showcase at least three content slides, but the technical solution
should be scalable to work with any number of content slides."

# 3D element in UI

After reading the instructions, one of the first issue I immediatly identified was the rendering of 3D element in UI, while keeping it responsive.
As I've had limited experiences with UI Toolkit, I didn't had a clue about its capabilities for such a case.

The several ideas I hade were : 

## Using a UI Toolkit feature ?
Wasn't sure, but after digging documentation and forums, I found nothing that could serve this purpose.

## Rendering UI in world space, and moving the object in front of the camera
That would have been the cheapiest, maybe not the easiest.
But as the background are just fixed image, it would have been a good solution.
I might have trouble with the responsiveness of the UI, but a single render texture might have done the trick (or camera viewport updating if I had time).
Sadly UI Toolkit doesn't support natively World Space nor Screen Space (Camera) rendering, it works only as overlay.
And I am not familiar enough with UI Toolkit to deeply hack it to emulate such a behaviour (regarding the estimated duration of the test)

## Using several cameras and render textures
The heaviest, but sadly the only way I found to make it work.

To mitigate the load of rendering several RT, I designed a system with only two RT, the first one being the current display and the second one being used for rendering the inactive slide we're sliding to.
Given the gesture of the player (be it up or down), I move the inactive slide at the expected position, set it's content via data and render it to the second RT.
It emulates the behaviour of a carousel but with only 2 slides.

# UI Hierarchy

For the UI hierarchy, I first thought to use a scrollview, has it has part of the expected behaviour, but I would have add several abstraction layers above my need (just 2 panes, moving vertically) and some features I won't need at all (scrollbars, for example).
I just used basic VisualElements, with absolute positioning, and implementend the inputs throught the UI Toolkit event system.
I only required 3 events : PointerDown, PointerUp and PointerMove (I added PointerLeave for case when the user leave the interactive zone).

# Sliding animation

I have the habit to use DoTween in almost all my projects, as it allows to quickly setup smooth animation for every imaginable case.
But I don't know if it really works well with UI Toolkit, and I have a single animation to do, so I decided to implement it with a Coroutine and work directly with the VisualElements styles.

# Scalibility

To assure scalability, I designed a data system base on a liste of "slide" with for each entry several informations : background image, dancer prefab, dance trigger name for the animator, ...
When I prepare a slide, I just retrieve these data from the list and setup the slide.

# Final thoughts
Regarding the test, I think the snapping of the slide after a transition might be smoothen, it's a bit rought as it is.
I've tried the project on my phone and it seems to run smoothly.
Now, the background is only a still picture, but if you ever want to render a full 3D scene, you might encounter performance issues.

Regarding UI Toolkit, my feeling is that it is not versatile enough (yet ?) for this purpose. I spent more time dodging its limits than actually use it as intended.
That's why I have heard from other devs about it and I tend to think the same. I will pursue my tests on it, but for a first try, I am not that much impressed.
