# Added multilingual menu
![prepomax_multilingual_ss001](https://github.com/user-attachments/assets/61263a6f-f660-4d67-951b-c873136da3c6)
![prepomax_multilingual_ss002](https://github.com/user-attachments/assets/d56fd442-0b6d-47a4-9c01-32d98f030e67)  
This repogitory is the test version for PrePoMax with mulitingual support.  
As an example, included Japanese.  
Similarly, you can implement any language you want.  
  
Not supported for all GUI components.  
The following items are already available.  
・Almost all of top menu bar and button  
・Almost all of tree GUI  
・Some dialog boxes  
For other components, you can do it in a similar way referring to the above components.  

[Demo video]  
<a href="https://youtu.be/608M0OK07ck"><img src="https://github.com/user-attachments/assets/03257b0e-32d7-49d7-b9dc-ffc38ce801f1" width="400px"></a>  
      
---original readme below ---  

# Prerequisites
*  Visual Studio 2022 Community (development environment) - https://www.visualstudio.com/downloads/
*  ActiViz OpenSource Edition 5.8.0 (64-bit Windows XP or later) (3D library used for graphics - must be installed only on the development PC; users do not need it) - https://prepomax.fs.um.si/downloads/

# PrePoMax Visual Studio setup
*  Download a Master branch of the PrePoMax package and extract it to a PrePoMax folder
*  Open the solution: "PrePoMax\PrePoMax.sln"

# Recreate the references to the VTK library
First delete the two existing references to the VTK library:
*  Open Solution Explorer: View->Solution Explorer
*  In Solution Explorer find the vtkControl project and then find the References branch
*  Delete the references starting with Kitware. (right click and Delete).
 
Secondly add the two references again using the following procedure:
*  Right click on References from vtkControl project in the Solution Explorer Window and selected Add Reference
*  A Reference manager window opens where you select Browse on the left side and then press the Browse button on the bottom right
*  Browse for the file Kitware.VTK.dll which should be in the ActiViz installation folder: "C:\Program Files\ActiViz.NET 5.8.0 OpenSource Edition\bin\Kitware.VTK.dll"
*  Repeat the procedure for the file Kitware.mummy.Runtime.dll: "C:\Program Files\ActiViz.NET 5.8.0 OpenSource Edition\bin\Kitware.mummy.Runtime.dll"

At last change the active solution platform using the main menu: **Build** -> **Configuration Manager** and select **x64** as the **Active solution platform**.

Start the compilation and execution of the project by pressing the Start button...

Compiling PrePoMax only creates some of its subfolders and default settings are prepared. To fully use a compiled version of PrePoMax, first look at the latest released version of the PrePoMax’s base folder. Then copy all folders that are missing in the compiled version from the released version (Models, NetGen, Solver…). Then you have to set the working folder and solvers (CalculiX) executables file name in the Settings->Calculix. In order to use the Gmsh mesher a file gmsh-4.12.dll must be copied from the release version lib subfolder to the compiled lib subfolder.

# Structure

The PrePoMax is a solution which consists of 11 projects:
*  CaeGlobals: global classes for all other projects to use
*  CaeJob: classes for running the analysis
*  CaeMesh: classes for FE mesh: nodes, elements, sets, ...
*  CaeModel: classes for FE model. Model contains FE mesh + materials, sections, ...
*  CaeResults: classes for FE results. Results contain FE mesh + field outputs, ...
*  GmshCommon: wrapper for the Gmsh mesher from https://github.com/tsvilans/gmsh_common 
*  GmshCaller: a stand alone executable used for running the Gmsh routines
*  PrePoMax: classes for user interface
*  STL: classes for stl geometry import
*  UserControls: classes for more complex user controls, as model tree view...
*  vtkControl: classes for 3D visualization

PrePoMax is compiled in an exe file all the other projects are compiled in dll files.

The internal structure of the program is quite complex and there are almost no comments in the code (no time to write them) but I am using very descriptive names. Each class has its own file with .cs extension. You can browse the files in the Solution Explorer.

The PrePoMax project has a Forms folder and inside it is a FrmMain.sc file/class. This is the main form. The form communicates exclusively with the Controller.sc file/class which holds all data about the model. The program records all user actions in order to be able to repeat them later (while running PrePoMax select Edit -> Regenerate) so all needed user functions/subroutines are not called directly but via Commands. There is a special Command class for each user action...
