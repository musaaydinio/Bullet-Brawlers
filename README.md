# 🔫 BULLET BRAWLERS - Full-Stack Multiplayer TPS & Web API Integration

![Unity](https://img.shields.io/badge/Unity-3D_Engine-000000?style=for-the-badge&logo=unity)
![Unity Netcode](https://img.shields.io/badge/Unity_Netcode-NGO_Multiplayer-blue?style=for-the-badge)
![.NET 9](https://img.shields.io/badge/.NET_9.0-REST_API-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-4169E1?style=for-the-badge&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED?style=for-the-badge&logo=docker)
![Render](https://img.shields.io/badge/Render-Cloud_Deployment-46E3B7?style=for-the-badge&logo=render)

**Bullet Brawlers** is a full-stack multiplayer Third-Person Shooter (TPS) project that seamlessly bridges client-side game engine dynamics with a production-ready cloud backend architecture. Built using **Unity Netcode for GameObjects (NGO)** and **Unity Gaming Services (UGS)** on the client, and a **Server-Authoritative ASP.NET Core (.NET 9) Web API** on the backend.

---

## 🔗 Live Links & Media Showcase

* 🎮 **Playable Game Build (itch.io):** [Bullet Brawlers on itch.io](https://nimugame.itch.io/bullet-brawlers)
* 🎬 **Gameplay & Network Sync Trailer:** [Watch Showcase Video](https://www.youtube.com/watch?v=2Zjn05duEP4)
* 🌐 **Live Web API (Swagger UI):** [Explore Live ASP.NET Core Endpoints](https://gamebackendapi-difs.onrender.com/swagger)
* 💻 **GitHub Source Code Repository:** [View Codebase on GitHub](https://github.com/musaaydinio/PC-Components-AP)
* 💼 **Developer LinkedIn:** [Musa Aydın LinkedIn Profile](https://www.linkedin.com/in/-musaaydin)

> ⚠️ **IMPORTANT NOTE (Backend Cold-Start Delay):** 
> The live REST API backend is deployed on a free **Render** cloud instance. If the server has been idle, **the very first login or authentication request may take 30 to 50 seconds** while the instance wakes up from sleep mode. Subsequent requests, inventory updates, and coin transactions will execute instantaneously.

---

## 📐 End-to-End System Architecture

┌─────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT SIDE (UNITY 3D)                            │
│  - Character Dynamics, TPS Camera & UI Management                           │
│  - Procedural Rigging (Spine Tilting via Camera Pitch)                      │
│  - 3D Spatial Audio Decay & Local/Proxy Component Isolation                 │
└───────────────────┬─────────────────────────────────┬───────────────────────┘
                    │                                 │
     Real-Time UDP Sync (DTLS)             HTTPS REST API (JWT Bearer)
                    │                                 │
                    ▼                                 ▼
┌───────────────────────────────────┐ ┌───────────────────────────────────┐
│     UNITY GAMING SERVICES (UGS)   │ │    ASP.NET CORE 9 WEB API        │
│  - UGS Relay (Allocation & Codes) │ │  - Layered / Clean Architecture   │
│  - UGS Lobby (Dynamic Room Query) │ │  - JWT Auth & HMACSHA512 Hashing  │
└───────────────────────────────────┘ │  - Business Logic & Economy Sync  │
                                      └─────────────────┬─────────────────┘
                                                        │
                                                 EF Core ORM
                                                        │
                                                        ▼
                                      ┌───────────────────────────────────┐
                                      │       POSTGRESQL (SUPABASE)       │
                                      │  - Encrypted User Credentials     │
                                      │  - Persistent Weapon Inventories  │
                                      │  - Player Coin Balances           │
                                      └───────────────────────────────────┘

## 🛠️ Part 1: Backend Architecture (ASP.NET Core Web API & Database)

The backend layer governs core data flow, authentication, and transactional business logic through a clean, layered RESTful Web API.

* **Data-Driven Architecture & EF Core:** Weapon attributes such as damage, rate of fire, magazine capacity, and prices are fully decoupled from client scripts. All data is managed in a PostgreSQL database via EF Core Migrations and dynamically fetched at startup.
* **Authentication & JWT Security:** Player authentication uses HMACSHA512 password hashing. Upon logging in via Unity, players receive a signed JWT Access Token, which authorizes all protected operations (market purchases, inventory updates, and reward commits).
* **Dynamic Market & Persistent Inventory Operations:** Players fetch available store items via `GET` requests. When purchasing a weapon (`POST`), the API verifies wallet balances, deducts coins on the server side, and writes the item to the user's persistent inventory table.
* **Server-Authoritative Economy & Match-End Sync:** To prevent client-side memory tampering (e.g., Cheat Engine), coins earned from match kills are calculated on the server. At game conclusion, the match state dispatches an authenticated HTTPS payload to `/api/Market/add-coins`, committing rewards safely to the database.

---

## ⚙️ Part 2: Multiplayer Network Architecture (Unity Netcode - NGO)

The client architecture provides a cheat-proof, low-latency Host-Client environment where players interact with synchronized movement, firing, and economic progression.

* **Dynamic Lobby & Matchmaking System (UGS Relay & Lobby):** Players can host lobbies that generate 6-digit Relay Join Codes or browse live active matches via the interactive UGS Lobby Browser (`QueryLobbiesAsync`), which automatically filters out full rooms.
* **Component Isolation & Input Handling:** Local player components (Camera, AudioListener, CharacterController) are activated exclusively for the owner (`IsOwner`). On proxy copies across the network, these components are disabled to prevent input overlapping and camera hijacking.
* **Server-Authoritative Combat System:** Projectile spawning and damage application are completely restricted to the server (`Host`). Firing requests pass through `ShootServerRpc`, validating the real sender's `ClientId` before spawning networked bullet instances.
* **Procedural Rigging & Animation Synchronization:** 
  * **Spine Bone Tilting:** In `LateUpdate`, camera pitch is procedurally applied to the character's spine bone (`spineKemigi`), providing accurate upper-body aim alignment without complex animation blending layers.
  * **Network Variables:** Locomotion speeds and grounded states are synchronized across proxies using `NetworkVariable<float>` and `NetworkVariable<bool>`.
* **Mid-Match Spawn Inventory Window:** During the first 5 seconds after spawning, players can open an in-game inventory (`B` Key) to switch active weapon loadouts, which fires a `ServerRpc` / `ClientRpc` cycle to update visual weapon models across all network clients.
* **3D Spatial Audio Architecture:** Local actions trigger 2D spatial sounds. For remote players, footstep and gunfire sounds are attenuated in 3D space (`spatialBlend = 1f`, linear decay from 2m to 40m) based on relative distance and direction.
* **Performance & Configuration Panel:** Features an in-game UI panel for tweaking FPS targets, mouse sensitivity, resolution scaling, and spatial audio volumes dynamically.

---

## 🛠️ Tech Stack Breakdown

### Client & Game Engine
* **Engine:** Unity 3D (URP, CharacterController, Input System)
* **Networking:** Unity Netcode for GameObjects (NGO), Unity Transport Protocol (UTP / DTLS)
* **Cloud Infrastructure:** Unity Gaming Services (Lobby, Relay, Anonymous Auth)
* **UI & Audio:** TextMeshPro, 3D Spatial Audio Attenuation

### Backend & Database
* **Framework:** C# / .NET 9 RESTful Web API
* **Architecture:** Layered / Clean Architecture with Repository Pattern
* **ORM & Database:** Entity Framework Core (EF Core), PostgreSQL (Supabase)
* **Security:** JWT Bearer Authentication, HMACSHA512 Password Hashing
* **Deployment:** Docker Containerization, Render Cloud Hosting

---

## 🎮 Game Controls

| Key / Input | Action |
| :--- | :--- |
| **W, A, S, D** | Character Movement |
| **Mouse** | Aim / Camera Control |
| **Left Click** | Fire Active Weapon |
| **Left Shift** | Sprint |
| **Spacebar** | Jump |
| **R** | Reload Magazine |
| **B** | Toggle In-Game Inventory / Weapon Switch (First 5s of Spawn) |
| **ESC** | Pause / Settings Menu |

---

## 🎥 Project Showcase Videos

### 1. Web API, Market, and Inventory Integration
Demonstrates bidirectional communication between Unity and the ASP.NET Core API: dynamic weapon fetching, JWT-authorized transactions, and equipping items from persistent cloud inventory.  

### 2. Gameplay, Network Sync, and Cloud Economy Loop
Encapsulates the full multiplayer gameplay loop across two simultaneous instances:
1. UGS Relay room creation and join code connection.
2. Low-latency movement, animation, and procedural spine synchronization.
3. Server-validated projectile hit detection and respawn loops.
4. **Match Conclusion:** Dispatching earned kill coins via authorized API calls and updating player database balances.  

---

## 👨‍💻 Author & Contact

**Musa Aydın** — *.NET Backend Developer & Game Developer*

* **LinkedIn:** [linkedin.com/in/musaaydin](https://www.linkedin.com/in/-musaaydin)
* **GitHub:** [github.com/musaaydinio](https://github.com/musaaydinio)
* **itch.io:** [musaaydinio.itch.io](https://nimugame.itch.io/)
