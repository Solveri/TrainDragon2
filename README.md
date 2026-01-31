1. Neural Network Architecture
The "brain" of the dragon is a feedforward neural network that maps environmental observations to physical actions:

Input Layer: The network receives raw data, including the dragon’s current velocity, its relative position to the target, and distances to nearby obstacles.

Hidden Layers: These layers process the non-linear relationships between the inputs (e.g., calculating how much lift is needed based on current momentum and altitude).

Output Layer: The network outputs continuous values that control the Left Wing Flap, Right Wing Flap, and Tail/Speed Control. By outputting differential values for the wings, the network enables the dragon to bank, turn, and stabilize itself.

2. The Training Process 
Using Unity ML-Agents and the PPO (Proximal Policy Optimization) algorithm, I simulated an evolutionary-style learning process:

Actions & Rewards: The dragon earns positive rewards for minimizing its distance to the target and maintaining flight smoothness. Conversely, it receives penalties for colliding with obstacles or falling below a certain altitude.

Optimization: Over thousands of episodes, the agent updates its neural weights to maximize its cumulative reward. This results in the emergence of complex maneuvers, such as tight turns to avoid pillars or diving to gain speed.

3. Performance Tracking & Model Persistence
To meet the project requirements for saving and reusing models, I implemented a NeuralNetworkSaver system:

Statistics: The system tracks key performance indicators (KPIs) like Success Rate, Average Reward, and Completion Time.

JSON Serialization: Once a training session reaches a high success threshold, the top-performing networks are serialized into JSON files.

Generalization: These saved weights can be loaded into entirely new environments with different obstacle layouts. This demonstrates that the dragon has truly learned the "physics" of flight and navigation rather than just memorizing a single path.
