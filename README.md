# Unity-Scripts

📁 Repository Description
This repository contains a curated collection of highly modular, reusable, and performance-optimized C# scripts developed for the Unity Engine (2020 LTS and later). These scripts are designed to address a wide range of game development scenarios, with emphasis on architectural scalability, maintainability, and adherence to industry-standard software engineering practices.

🔧 Core Features
Component-Based Architecture: All scripts are structured following the SOLID principles and utilize Unity's ECS-compatible paradigms where applicable.

Editor Tooling Integration: Includes custom Editor windows, property drawers, and inspector attributes to streamline workflows and reduce manual overhead.

Asynchronous Task Handling: Utilizes async/await, coroutines, and event-driven patterns to optimize runtime performance and reduce thread contention.

Memory Management: Implements object pooling, lazy instantiation, and strategic GC minimization for mobile and console platforms.

Multiplatform Compatibility: Scripts are tested and validated across multiple build targets, including PC, Android, iOS, and WebGL.

Decoupled Event Systems: Implements both observer-based and signal-based event dispatchers with full support for dependency injection frameworks (e.g., Zenject).

Physics and Animation Utilities: Abstracted layers over Unity’s Animation and Rigidbody systems to enable deterministic control and dynamic runtime modification.

📐 Design Patterns and Paradigms
Command Pattern

State Machine (FSM and HFSM implementations)

Factory and Singleton (Thread-safe)

Observer and Mediator

Service Locator and Dependency Injection

Strategy and Decorator

ScriptableObject-based architecture for runtime configuration

📚 Documentation & Usage
Each script is thoroughly documented with XML comments and includes usage examples, editor configuration notes, and implementation guidelines. Please refer to the /Documentation folder or individual script headers.

⚠️ Requirements
Unity 2020.3 LTS or newer (compatible with Unity 2022+)

.NET Standard 2.1

Burst Compiler and Jobs package (for performance-critical modules)

Input System package (for input-related utilities)
