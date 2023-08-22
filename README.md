# Description

Grid Road Tool is an easy-to-use road generation tool that allows for the creation simple flat grid-based street layouts. Once set up, all you have to do is click and drag to start adding and removing roads!
![](https://github.com/Omurex/Unity_GridRoadTool/blob/main/README_Gifs/ToolInAction.gif)

# How to Install
1. In Unity, click on the *Window* tab, and then select *Package Manager* from the dropdown.
2. Click the plus sign in the top left of the Package Manager window, and click *Add package from git URL*.
3. Copy and paste the HTTPS link for the repo (https://github.com/Omurex/Unity_GridRoadTool.git), and click "Add".

# How to Set Up
1. Place a terrain in your scene and set its layer to a layer called "Terrain".
2. Click on the *Tools* tab along the top row of the Unity Editor window, and click *Road Generation Tool* in the dropdown.
3. A window will popup. Click on the terrain you placed, and this will spawn a "RoadGrid" object as a child of the terrain.
4. The window will change to show multiple configurable settings for the road system. Click on the *Initialize Road Grid* button.
5. A grid will now appear over the terrain. You're now able to select different boxes in that grid and start drawing roads!
![](https://github.com/Omurex/Unity_GridRoadTool/blob/main/README_Gifs/CreateRoadGrid.gif)

# How to Use
Once the grid is set up on the terrain, there are two ways to create roads:
   1. Click and drag
   2. Manually change a road point's connected directions

## 1. Click and Drag
To add a new road, click and drag in a straight horizontal or vertical line across empty boxes, or boxes with roads not travelling the same direction as your drag.
![](https://github.com/Omurex/Unity_GridRoadTool/blob/main/README_Gifs/DragAddRoad.gif)

To remove a road, click and drag in a straight horizontal or vertical line across boxes with roads in it going in the same direction as your drag.
![](https://github.com/Omurex/Unity_GridRoadTool/blob/main/README_Gifs/DragRemoveRoad.gif)

## 2. Manually Change Points
When you click on a point in the grid, the editor window will show a changeable flag enum called *Road Point Connected Directions*. Changing this will either add or remove roads going in the direction changed. For example, if the point has no flags selected (says NONE), selecting North will cause roads to be spawned going north. The roads will spawn until either reaching the end of the grid or hitting a road point with already existing roads.
![](https://github.com/Omurex/Unity_GridRoadTool/blob/main/README_Gifs/PointAddAndRemove.gif)

# License
MIT License

Copyright (c) 2023 Joseph Lyons

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

# Links and Contact
[Repository](https://github.com/Omurex/Unity_GridRoadTool)
[Joseph Lyons (Author) Email](josephlyons.professional@gmail.com)

Please let me know of any issues, and I'll work on getting them fixed. Alternatively, if you wanted to fix any bugs / add any features, feel free to submit a pull request.
