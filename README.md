# asv-drones-gbs
A ground base station service with Real-Time Kinematic (RTK) support

![image](https://github.com/asv-soft/asv-drones-gbs/assets/1770739/32cd7421-56a0-4a39-b365-55de64e512ac)

## 2. Getting Started

### Setting Up the Development Environment

To ensure a smooth development experience, follow the steps below to set up your development environment:

#### 1. **Prerequisites:**
- **Operating System:** This project is compatible with Windows, macOS, and Linux. Ensure that your development machine runs one of these supported operating systems.
- **IDE (Integrated Development Environment):** We recommend using [Visual Studio](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) as your IDE for C# development. Make sure to install the necessary extensions and plugins for a better development experience.

#### 2. **.NET 7 Installation:**
- This project is built using [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0), the latest version of the .NET platform. Install .NET 7 by following the instructions provided on the official [.NET website](https://dotnet.microsoft.com/download/dotnet/7.0).

   ```bash
   # Check your current .NET version
   dotnet --version
   ```

#### 3. **Version Control:**
- If you haven't already, install a version control system such as [Git](https://git-scm.com/) to track changes and collaborate with other developers.

#### 4. **Clone the Repository:**
- Clone the project repository to your local machine using the following command:

   ```bash
   git clone https://github.com/asv-soft/asv-drones-gbs.git
   ```

#### 5. **Restore Dependencies:**
- Navigate to the project directory and restore the required dependencies using the following command:

   ```bash
   cd asv-drones-gbs
   dotnet restore
   ```

#### 6. **Open in IDE:**
- Open the project in your preferred IDE. For Visual Studio Code, you can open the project by typing:

   ```bash
   code .
   ```

#### 7. **Build and Run:**
- Build the project to ensure that everything is set up correctly:

   ```bash
   dotnet build
   ```

- Run the project:

   ```bash
   dotnet run
   ```

Congratulations! Your development environment is now set up, and you are ready to start contributing to the project. If you encounter any issues during the setup process, refer to the project's documentation or reach out to the development team for assistance.

## 3. Code Structure

The organization of the codebase plays a crucial role in maintaining a clean, scalable, and easily understandable project. This section outlines the structure of our codebase, highlighting key directories and their purposes.

