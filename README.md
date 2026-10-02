# Bullet Brawlers - Full-Stack Multiplayer TPS & Web API Integration

Bullet Brawlers is a comprehensive Full-Stack multiplayer Third-Person Shooter (TPS) project that integrates game engine mechanics with backend software architecture end-to-end. 

The primary goal of this project is to establish a bidirectional communication bridge between a Host-Client network architecture built with Unity Netcode for GameObjects (NGO) and a custom **ASP.NET Core Web API**. The system synchronizes real-time combat mechanics over the network while securely managing player data, authentication, and in-game economy through the Web API on an SQL database.

---

## 🛠️ Part 1: Backend Architecture (ASP.NET Core Web API & Database)
The core data flow and security are managed via a Web API designed according to layered architecture principles. The client communicates with this API for all critical data.

* **Data-Driven Architecture & EF Core:** Weapon attributes such as damage, fire rate, magazine capacity, and prices are not hardcoded. All attributes are stored in the SQL database using EF Core Migrations and dynamically fetched via the API when the game is launched.
* **Authentication & JWT Security:** Players registering or logging into the system via Unity receive a JSON Web Token (JWT). This token is used to authorize all sensitive operations, such as market purchases and inventory management.
* **Dynamic Market and Inventory Operations:** Weapon data is fetched from the database via a `GET` request. When a player purchases a weapon, a `POST` request is sent to the API; the API verifies the balance, deducts the cost, and writes the weapon to the player's persistent inventory table.
* **Centralized Economy & Match-End Sync:** To prevent client-side cheating, balance updates are calculated strictly by the business logic within the API. When a game loop concludes, the earned Coins are transmitted to the Web API and committed to the persistent SQL database, ensuring data integrity between sessions.

---

## ⚙️ Part 2: Multiplayer Network Architecture (Unity Netcode - NGO)
My primary focus on the client side was to establish a stable Host-Client architecture and ensure a cheat-proof, synchronized environment where players can interact without latency.

* **Component Isolation & Input Management:** I isolated the camera, physics, and audio listener components to prevent input overlapping among players on the network. By keeping the local player's controls active while disabling these components on networked proxy copies, I ensured a seamless and fluid experience.
* **Server-Authoritative Combat System:** To prevent client-side manipulation, I moved critical mechanics like bullet spawning and hit registration entirely to the server (Host). By validating the true identity of the shooter on the server before spawning a bullet, I secured the game against fraudulent damage reports.
* **Seamless Animation Synchronization:** Character states such as walking, running, and jumping were synchronized instantly across the network. A physical movement initiated by one player is smoothly and immediately reflected on the animators of all other players on the network.
* **3D Spatial Audio Architecture:** I built a dynamic audio management system based on player position. While a player hears their own footsteps and gunshots directly (in 2D), sounds from other networked players are processed in 3D space (Spatial Audio) based on their distance and direction.
* **Performance and Settings Management:** To maintain optimal performance during multiplayer sessions, I designed an in-game configuration panel allowing players to dynamically adjust FPS limits, resolution scaling, and audio volumes.

---

## 🎥 Project Showcase Videos

### 1. Web API, Market, and Inventory Integration
This video tests the communication between the Web API and Unity. It demonstrates fetching weapon data from the database into the interface, the JWT-authorized purchase process, and equipping a weapon from the persistent inventory.
👉 **[VIDEO_1_LINK_HERE]**

### 2. Gameplay, Network Sync, and API Economy Conclusion
This video encapsulates the end-to-end loop of the project:
1. Running the game simultaneously in two separate instances (Host and Client).
2. Flawless synchronization of character movements and animations on each other's screens.
3. Server-validated hit detection and the death/respawn loop.
4. **Match End:** Sending the earned Coins via a `POST` request to the Web API and securely updating the player's balance in the database.
👉 **[https://lnkd.in/p/dDDd6wae]**

---

## 📈 Developer's Note
This project is the direct result of over a year of game development in Unity combined with my 6-month focus on backend software architectures. My goal was to demonstrate that I could build the gameplay mechanics while simultaneously establishing the underlying Host-Client network infrastructure and a secure database management system (Web API) from scratch. Bullet Brawlers is the most comprehensive project reflecting my current Full-Stack capabilities.
