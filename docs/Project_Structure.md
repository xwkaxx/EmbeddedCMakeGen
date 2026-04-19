# EmbeddedCMakeGen Project Structure

## Solution Layout

```text
EmbeddedCMakeGen/
├─ EmbeddedCMakeGen.sln
├─ README.md
├─ docs/
│  ├─ V1_Boundary.md
│  ├─ Project_Structure.md
│  └─ Codex_Task_Plan.md
│
└─ EmbeddedCMakeGen/
   ├─ Program.cs
   ├─ Commands/
   ├─ Application/
   │  ├─ Services/
   │  └─ Models/
   ├─ Domain/
   │  ├─ Models/
   │  ├─ Interfaces/
   │  └─ Constants/
   ├─ Infrastructure/
   │  ├─ Scanning/
   │  ├─ Analysis/
   │  ├─ Generation/
   │  ├─ IO/
   │  ├─ Pathing/
   │  ├─ Logging/
   │  └─ Validation/
   └─ Templates/
