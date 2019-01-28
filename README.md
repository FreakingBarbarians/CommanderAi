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

I used an earlier (and much uglier) version of this system that enabled merchant vessles to dynamically find profitable trade routes in a ship game I was building a few months ago.

Here are some screenshots.

![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/0.PNG "The ship, loaded with iron ore makes way for the city with the forge")

This is an example of the older (uglier) version running on top of a city simulation I made. The city circled in `red` has an `iron mine`, producing `iron ore`. The city uncircled has an `iron forge` producing `iron` from `iron ore`. These items are data driven and described in json with hooks into the simulation system. But out of the scope of this readme.

The flag (placeholder for a fleet of boats) in the middle has just traded a bunch of `iron ore` from the `red` city. We can see circled in green the state transitions for the boat's brain.

(You may have to view the image in fullscreen to read the text)

Of note are the transitions circled in green.
```
node_trade > SUCCESS > node_gather
```
The `node_trade` succeeds in buying `iron ore` from the `red` city. Then it returns to the `node_gather` which is responsible for gathering the target trade good, in this case `iron ore`.

```
node_gather > INSUFFICIENT > node_move
```

The `node_gather` checks if the fleet has enough of the target trade good to make the trade deal, with some hardcoded heuristics that ensure profitibility (although trade goods are currently zero cost so it's kind of redundant c:). The `node_gather` also checks through the known cities of the ship. If there is a city that sells the trade good it sets an internal `target_city` variable and returns `INSUFFICIENT`, meaning that there is not enough trade good to proceed to the selling phase, but we can acquire it somehow. If there was no city selling `iron ore` then the return would have been `node_fail`. `node_move` moves the fleet to the target city.

```
node_gather > SUCCESS > node_move
```

The fleet has enough `iron ore` to make a trade and now goes to the target `sell city`.


![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/1.PNG "Profit! We have satisfied the rules of acquisition! The Grand Nagus be praised!")

In this image our fleet has made it to the city with the forge circled in `black`. The `black` city is equipped with a forge, and can process `iron ore` into `iron`.

In note are the transitions circled in blue.

```
node_move > SUCCESS > node_trade
```

The fleet successfully reached it's destination and has begun trading.

```
node_trade > SUCCESS > node_wait_5
```
The fleet successfully trades and waits for 5 seconds.

![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/2.PNG "After a short break the fleet resumes its quest for profits!")

After the short wait the fleet begins to look for trade deals again. This process involves inspecting the trade tables of each city, which are dynamically created based on the buildings in each city and the input/output resources as well as resource amounts. There are a lot of resource types and trade deal types but again that is out of the scope of this readme.

```
node_wait_5 > SUCCESS > node_get_deals
```

Waiting is finished, now it proceeds to scan the trade tables for any buy/sell pair that is profitable.

```
Picked: good_ore
```

The `node_pick_deals` has picked `good_ore` as it's trade target from it's profitable pairs.

```
node_gather > INSUFFICIENT > node_move
```

As before, the cycle continues.


There are also some unit tests that prove the basic functionality of the program.
https://github.com/FreakingBarbarians/CommanderAi/blob/master/UnitTestProject1/UnitTest1.cs

### Bonus A*

Incidentally this project also had an A* search that I forgot I implemented.


![Image failed to load :(](https://raw.githubusercontent.com/FreakingBarbarians/CommanderAi/master/Images/3.PNG "One needs to navigate the harsh seas in search of money!")

In the background is a staggered grid with connections to it's neighbors. And anything within the green land is unpathable.
