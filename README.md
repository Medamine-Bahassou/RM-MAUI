# MAUI Restaurant Management System - TP

![MAUI Logo](https://img.shields.io/badge/-.NET%20MAUI-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![.NET 8](https://img.shields.io/badge/-.NET%208-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![MySQL](https://img.shields.io/badge/-MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)

This repository contains a .NET MAUI application developed as a practical work (Travaux Pratiques) for managing restaurant operations. The application aims to provide core functionalities required by both restaurant staff and customers.

## Project Description

This project is a practical exercise focused on building a cross-platform restaurant management application using .NET MAUI and a MySQL database. It covers essential aspects of restaurant operations, from managing the menu and orders to handling reservations and user accounts.

## Features

The application includes the following key features:

✨ **Menu Management:**
*   Categorize dishes.
*   Manage individual dishes with names, descriptions, and prices.

🛒 **Ordering System:**
*   Browse the menu.
*   Add items to a shopping cart.
*   Manage quantities in the cart.
*   Place orders.

📅 **Table Reservations:**
*   View available tables.
*   Make and manage table reservations.

👤 **User Profiles:**
*   Support for different user roles (Clients and Staff).
*   User registration and authentication (Login/Logout).

🔔 **Notifications:**
*   System for sending and receiving notifications (e.g., order status updates, reservation confirmations).

💳 **Simulated Payments:**
*   Basic simulation of the payment process for orders.

📊 **Statistics and Reports:**
*   Functionality to view statistics and reports related to sales, orders, or reservations.

## Technologies Used

*   **Framework:** .NET 8
*   **UI Framework:** .NET MAUI (Cross-Platform)
*   **Language:** C#
*   **Database:** MySQL
*   **ORM:** Entity Framework Core
*   **MySQL Provider:** Pomelo.EntityFrameworkCore.MySql
*   **MVVM:** CommunityToolkit.Mvvm
*   **UI Extensions:** CommunityToolkit.Maui
*   **Security:** BCrypt.Net-Next (for password hashing)
*   **Dependency Management:** NuGet

## Prerequisites

Before running this project, ensure you have the following installed:

1.  **.NET 8 SDK:** Download and install the latest .NET 8 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).
2.  **Visual Studio 2022 or Rider:** An IDE that supports .NET MAUI development.
    *   If using Visual Studio 2022, make sure the ".NET Multi-platform App UI development" workload is installed.
    *   If using Rider, ensure the .NET MAUI plugin is enabled.
3.  **MySQL Server:** A running instance of MySQL Server.
4.  **MySQL Client:** A tool like MySQL Workbench or a command-line client to interact with your MySQL database.

## Setup and Installation

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/Medamine-Bahassou/RM-MAUI.git
    cd RM-MAUI
    ```

2.  **Database Configuration:**
    *   Create a new database in your MySQL server (e.g., `restaurant_db`).
    *   Locate the database connection string within the project (often found in `appsettings.json` or a configuration class). Update the connection string to point to your MySQL server instance, database name, user ID, and password.
    *   Open a terminal in the project directory (`RM-MAUI`).
    *   Apply the Entity Framework Core migrations to create the database schema:
        ```bash
        dotnet ef database update
        ```
        *Note: You might need to install the `dotnet-ef` tool globally: `dotnet tool install --global dotnet-ef`*

3.  **Restore Dependencies:**
    Dependencies should be automatically restored when you open the project in an IDE, but you can also run:
    ```bash
    dotnet restore
    ```

4.  **Build and Run:**
    *   Open the project in your chosen IDE (Visual Studio 2022, Rider).
    *   Select the desired target platform (e.g., `Windows Machine`, Android Emulator, iOS Simulator).
    *   Build and run the project using the IDE's interface.

    Alternatively, you can run from the command line (example for Windows):
    ```bash
    dotnet build
    dotnet run -f net8.0-windows10.0.19041.0
    ```
    Replace `net8.0-windows10.0.19041.0` with the target framework identifier for your desired platform if not running on Windows.

## Usage

1.  Launch the application.
2.  Upon first use, you may need to register a new user account. The application might have seed data or require manual addition of initial data (like staff accounts, categories, dishes) through an administrative interface (if implemented) or directly in the database.
3.  Explore the different sections based on the logged-in user's role (Client or Staff).

## Project Status

This project was developed as a practical exercise. It includes the core functionalities listed above, but may require further development, testing, and refinement for production use.

## Contact

For questions or feedback, please open an issue on this GitHub repository or contact the repository owner:

*   GitHub: [Medamine-Bahassou](https://github.com/Medamine-Bahassou)

---
© [Year, e.g., 2024] Medamine Bahassou. All rights reserved.
