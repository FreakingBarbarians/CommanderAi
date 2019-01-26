# CommanderAi

Commander Ai is a multi-threaded state driven AI solution. It aims to be

- Easy to use
- Scalable
- Versatile
- Engine Agnostic

> Although it is in it's early stages so none of that may be true yet :)

## Mind Over Matter

The engine plays on three main ideas.

- Separation of World Space and AI Space
- State Machines
- Orders and Order Results

Each `GameObject` that is controlled by an AI has an `AIBrain` in the engine. The `AIBrain` runs an `AIState` machine that either preforms some _thinking_ operation or gives `Orders` to the `GameObject` or both.

![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/Fig_0.png "Bob hard at work")

In this simplified representation we can see `Bob` being _ordered_ to _cut wood_. The `AIBrain` controlling `Bob` endlessly has him cycle between cutting wood and piling it.

The `gameobject` performs the execution of each order *synchronously* with the gameloop. When the order terminates it returns an `OrderResult` to the `AIBrain`. The `AIBrain` then uses the `OrderResult` to advance it's `AIState` machine.

![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/Fig_1.png "Bob's gotta think a bit harder now.")

In the above example. Bob's `AIBrain` sends Bob's `GameObject` a `FindWood` `Order`. The order casts a circle around bob of radius `r`, mutating the `AIState`'s `target_wood` variable and returning `success`. The `AIBrain` transitions the `current_node` to `PathToWood` which does some AI space _thinking_ in the form of an A* from Bob's `GameObject` position to the state variable `target_wood`. Due to the brick wall separating Bob from the Tree and a lack of a third dimension the A* would inevitably fail. The `current_node` itself would return `failure` to the `AIBrain` causing a transition back to `FindWood`.

Both the `AIState` and the individual nodes in the state can hold variables. The nodes within a state can also inspect the variables of other nodes in the same state. This can lead to interesting behavior such as branching after a certain node has reached some condition (i.e. has been reached 10 times or more).

## Why Multithreading

Multithreading brings several advantages such as making full use of more common multicore processors and amortizing the cost of expensive operations (A*) over time. Most game engines' main simulation loops are still single-threaded, to slap expensive AI operations on top can lead to noticable performance drops. To illustrate, if A* takes roughly 100ms to resolve on average, if we put the A* operation in the main loop then each time an actor decides to path to a new position the game would freeze for 1/10th of a second. This is quite unacceptable. But if we execute the A* operation on a separate thread then the game will not freeze and after 100ms (maybe a little bit more) the actor will begin to move, and this is generally unnoticable or at the very least midly annoying.

Commander AI implements multithreading by assigning `AIBrains` to worker threads, it attempts to find the least loaded thread to perform load balancing. Currently there is no way to load re-balance after `AIBrains` have been added and removed repeatedly but the framework and metrics are there to make this easy to implement.

## Examples of Use

I used an earlier (and much uglier) version of this system that enabled merchant vessles to dynamically find profitable trade routes in a ship game I was building a few months ago. (Remind myself to upload a video of this later)

There are also some unit tests that prove the basic functionality of the program.
https://github.com/FreakingBarbarians/CommanderAi/blob/master/UnitTestProject1/UnitTest1.cs
