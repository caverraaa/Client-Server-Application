# Client-Server Application with C#

A multi-threaded client-server application for managing confectionery manufacturing sales. Built with C# and .NET, this project demonstrates high-performance data handling and network communication.

## 🏗️ Architectural Overview
The system follows a **Client-Server architecture**:
- **Server:** A high-performance backend that handles concurrent client requests using **multi-threading**. It manages data persistence via file-based storage and ensures data integrity during simultaneous operations.
- **Client:** A GUI-based desktop application (WinForms) providing a rich interface for CRUD operations, real-time data filtering, and search logic.

## ⚡ Key Engineering Features
- **Asynchronous Communication:** Implemented custom socket/TCP logic for efficient data exchange between components.
- **Multi-threaded Request Handling:** The server is capable of processing multiple client connections simultaneously without blocking.
- **Data Persistence Layer:** Custom implementation of data storage with support for Create, Read, Update, and Delete (CRUD) operations.
- **Search & Filtering Engine:** Advanced search logic implemented on the client side to filter through sales records.

## 🛠️ Tech Stack
- **Language:** C#
- **Platform:** .NET
- **Networking:** Sockets / TCP/IP
- **Data Format:** JSON / Plain Text
- **GUI:** Windows Forms
