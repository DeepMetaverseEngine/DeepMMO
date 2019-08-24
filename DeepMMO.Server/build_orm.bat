echo off
@SET ProjectDir=%1
@SET SolutionDir=%2
@SET TargetDir=%3
@echo ---------------------------------------------------------------------------
@echo - Copy To : %TargetDir%
@echo ---------------------------------------------------------------------------
@IF EXIST %SolutionDir%..\DeepCore\Library\bin\Debug (
@xcopy /Y %SolutionDir%..\DeepCore\Library\bin\Debug\codegen.exe               %TargetDir%
@xcopy /Y %SolutionDir%..\DeepCore\Library\bin\Debug\csharp-*.xml              %TargetDir%
)
@echo ---------------------------------------------------------------------------
@echo - GEN
@echo ---------------------------------------------------------------------------
@set gen_ref=%TargetDir%DeepCore.dll
@set gen_ref=%gen_ref%;%TargetDir%DeepMMO.dll
@set gen_ref=%gen_ref%;%TargetDir%DeepMMO.Server.dll
@del /Q %ProjectDir%..\DeepMMO.ORM\generated_orm\*.cs
@%TargetDir%codegen -ns:DeepMMO.ORM -wd:%TargetDir% -if:%gen_ref% -od:%ProjectDir%..\DeepMMO.ORM\generated_orm         -t:csharp-orm.xml 
@%TargetDir%codegen -ns:DeepMMO.ORM -wd:%TargetDir% -if:%gen_ref% -of:%ProjectDir%..\DeepMMO.ORM\generated_orm\auto.cs -t:csharp-orm-ids.xml 
@rem dotnet build -o %TargetDir% %ProjectDir%
@echo CSC: %ERRORLEVEL%
@echo ---------------------------------------------------------------------------
