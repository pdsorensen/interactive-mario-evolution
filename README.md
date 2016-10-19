# Interactive Mario Evolution 

## Description

- TODO: link paper and video 
- TODO: provide description of project  

## Installing in eclipse
you can install in any IDE you prefer, but we used good old Eclipse. 

### Setting up the Mario engine: 
1. Create two empty projects, one for the Mario Engine and one for the ASM library
2. Copy the files from the /org folder to the /src folder in your ASM project 
3. Copy the files from the /mario folder to the /src folder in your Mario project 
4. add missing libraries by rightclicking on mario project -> go into properties -> java build path -> libraries and add all the libraries in the /libraries folder. Now you should be able to run the HumanMarioPlayer in the package own.util   

### Packages
- Idsia packages contains experiments, mario engine components, and other tools specifically related to the Mario Engine written in Java. 

- Amico packages contains experiments, mario engine components and other tools specifically related to the Mario Engine written in Python. 

- anji packages contains experiments based on the ANJI framework. Some which might need to be run from the command line. For more info see (LINK) 

- Jgap contains genetic algorithms, for more info see http://jgap.sourceforge.net. 

- "iec" and "own" packages contains experiments, mario engine components, and other tools related to the Mario IEC experiment presented in this paper: (LINK) 

## Running a IEC experiement: 
### Setting up the folder structure for file streams: 
1. In your root folder of the Mario project (same place as src and bin is located) created a folder called db. 
2. in the db folder, create five folders called "best", "chromosome", "gifs", "images" and "run". Also, create a folder called "levelImages" in the "images" folder. 

## Tools and utility: 
the evolutionary parameters is provided by a .properties file. Ours is called and mario.properties. Each field is described at: LINK 

The original ui debugger has been commented out and replaced with a more ANN freindly ui debugger. To get the original one back, go into "idsia/benchmark/mario/engine/MarioVisualComponent.java" and comment/uncomment the tick function, to get whatever UI you desire.

The MarioVisualComponent also contains code for saving images, creating gifs specifically related to the IEC experiment. 