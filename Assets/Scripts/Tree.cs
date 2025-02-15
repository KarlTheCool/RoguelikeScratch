using System;
using System.Collections.Generic;
using Random = System.Random;

/*
 *  Tree
 *
 *  Purpose: Generate dungeon maps for the level generator to use as a blueprint.
 *  Tree generates a 2 dimensional (jagged) array of numbers, like 0 for wall and 1 for floor.
 *  It does not handle anything to do with the Unity game engine. LevelGenerator takes
 *  maps generated by this class's methods and uses them to draw the tile map in the game.
 */
public class Tree
{
    // Constants
    public const int DungeonWidth   = 80; // In tiles
    public const int DungeonHeight  = 40;
    private const int SplitMargin    = 1; // How many tiles between rooms
    private const int CorridorWidth = 1; 
    private const int SplitIterations = 3; // Dungeon will have 2^SplitIterations. So if this is 3, 8 rooms.
    
    private TreeNode _root;
    private List<TreeNode> _leafNodes;
    private List<Area> _roomList;

    private Random _random = new Random();
    
    public Tree()
    {
        _root = new TreeNode(null);
        _root.Data = new Area(0, 0, DungeonWidth, DungeonHeight);
        _leafNodes = new List<TreeNode>();
        _leafNodes.Add(_root);
        _roomList = new List<Area>();
    }
    
    // Split each leaf. Make the leafs children the new leafs.
    public void SplitAll()
    {
        List<TreeNode> newLeafNodes = new List<TreeNode>();

        foreach (TreeNode leaf in _leafNodes)
        {
            Split(leaf);
        }

        foreach (TreeNode leaf in _leafNodes)
        {
            if (leaf.GetLeftChild() != null)
                newLeafNodes.Add(leaf.GetLeftChild());
            if (leaf.GetRightChild() != null)
                newLeafNodes.Add(leaf.GetRightChild());
        }

        // Replace the list containing the 2nd lowest level
        // with the one containing the new leaf level.
        _leafNodes = newLeafNodes;
    }

    public void Split(TreeNode node)
    {
        TreeNode newLChild = new TreeNode(node);
        TreeNode newRChild = new TreeNode(node);
        
        node.SetLeftChild(newLChild);
        node.SetRightChild(newRChild);

        // choose a random direction : horizontal or vertical splitting
        bool splitVertically = _random.Next(10 + 1) < 5;
        if (splitVertically)
        {
            HalveVertically(node, newLChild, newRChild);
        }
        else
        {
            HalveHorizontally(node, newLChild, newRChild);
        }   
    }

    #if !UNITY_EDITOR
    // Prints a dungeon layout in ASCII to the terminal.
    // Used in testing with dungeontest.sh
    public void PrintMapArr(int[,] mapArr)
    {
        for (int y = 0; y < DungeonHeight; y++)
        {
            for (int x = 0; x < DungeonWidth; x++)
            {
                int v = mapArr[x, y];
                switch (v)
                {
                    case 0:
                        Console.Write('█');
                        break;
                    case 1:
                        Console.Write('.');
                        break;
                }
            }
            Console.WriteLine();
        }
    }
    #endif

    public int[,] MakeMapArr(List<Area> areaList)
    {
        int[,] mapArr = new int[DungeonWidth, DungeonHeight];

        Console.WriteLine("Size of arr: " + mapArr.Length);

        Area currentArea;
        
        for (int i = 0; i < areaList.Count; i++)
        {
            currentArea = areaList[i];

            for (int y = 0; y < currentArea.H; y++)
            {
                for (int x = 0; x < currentArea.W; x++)
                {
                    mapArr[currentArea.X + x, currentArea.Y + y] = 1;
                }
            }
        }

        return mapArr;
    }

    // Takes a fully partitioned list of nodes with areas and put a room in each one.
    // Returns a list of Areas describing rooms. Essentially randomly carves out a room within
    // the bounds of each Area given by the Data members of nodes in the node list.
    public void CreateRooms(List<TreeNode> leafNodesList)
    {
        TreeNode currentNode;
        Area currentArea;
        // For each node in the node list:
        for (int i = 0; i < leafNodesList.Count; i++)
        {
            // Get current Area.
            currentNode = _leafNodes[i];
            currentArea = currentNode.Data;
            
            // Pick a random x and y offset, both within width/2 and height/2.
            int xOffset = _random.Next(1, currentArea.W / 2);
            int yOffset = _random.Next(1, currentArea.H / 2);
            
            // Determine right and upper bounds based on the selected room offsets
            int widthBound = currentArea.W - xOffset;
            int heightBound = currentArea.H - yOffset;
            
            // Select width and height within the bounds
            int roomWidth = _random.Next(widthBound / 2, widthBound - SplitMargin);
            int roomHeight = _random.Next(heightBound / 2, heightBound - SplitMargin);
            Area newRoom = new Area(
                currentArea.X + xOffset,
                currentArea.Y + yOffset,
                roomWidth,
                roomHeight);
            _roomList.Add(newRoom);
            
        }
    }
    
    // Takes a list of rooms and adds to the list a series of corridors between rooms.
    public void CreateCorridors(List<Area> roomList)
    {
        // Shuffle the list
        int n = _roomList.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            Area val = _roomList[k];
            _roomList[k] = _roomList[n];
            _roomList[n] = val;
        }
       
        // Corridors are stored in a separate list so that _roomList is not modified while it is being
        // read.
        List<Area> corridors = new List<Area>();

        for (int i = 0; i < _roomList.Count - 1; i++)
        {
            Area roomA = _roomList[i];
            Area roomB = _roomList[i + 1];

            Area pointA = RandPointWithin(roomA);
            Area pointB = RandPointWithin(roomB);
            
            // Create a rectangular area that spans the width.
            Area widthSpan;
            int corridorLength;
            
            // If A is to the left of B
            if (pointA.X < pointB.X)
            {
                // then length is B.X - A.X
                corridorLength = pointB.X - pointA.X;
                widthSpan = new Area(pointA.X, pointA.Y, corridorLength, CorridorWidth);
            }
            // If B is to the left of A
            else
            {
                // then length is A.X - B.X
                corridorLength = pointA.X - pointB.X;
                widthSpan = new Area(pointB.X, pointB.Y, corridorLength, CorridorWidth);
            }
            
            corridors.Add(widthSpan);
            
            // Create a rectangular area that spans the height.
            Area heightSpan;
            int corridorHeight;
            // If A is above B
            if (pointA.Y < pointB.Y)
            {
                // then height is B.Y - A.Y
                corridorHeight = pointB.Y - pointA.Y;
                heightSpan = new Area(pointA.X , pointA.Y, CorridorWidth, corridorHeight);
            }
            // If B is above A
            else
            {
                // then height is A.Y - B.Y
                corridorHeight = pointA.Y - pointB.Y;
                heightSpan = new Area(pointB.X, pointB.Y, CorridorWidth, corridorHeight);
            }
            corridors.Add(heightSpan);
        }
        
        // Now that we are finished adding corridors, we can safely add them all to _roomList.
        foreach (Area corridor in corridors)
            _roomList.Add(corridor);
    }

    private Area RandPointWithin(Area room)
    {
        Area point = new Area(0, 0, 0, 0);
        point.X = _random.Next(room.X + 1, room.X + room.W + 1);
        point.Y = _random.Next(room.Y + 1, room.Y + room.H + 1);
        return point;
    }

    private void HandleUnreachableAreas(int[,] map, List<Area> roomList)
    {
        // TODO: Convert the map to a graph represented by an unweighted adjacency list.
        
        // TODO: Take the first room in the room list, and iterate over all other rooms.
        
        // TODO: If the rooms are all connected, a path will exist. If not, the map needs to be regenerated.
        // OR, an extra corridor could be drawn to that inaccessible room.
    }
    
    // Split `node` into exact halves along a vertical line. No random generation of room dimensions.
    private void HalveVertically(TreeNode node, TreeNode newLChild, TreeNode newRChild)
    {
        Area toDivide = node.Data;
        
        // Compute the left half of the area.
        Area leftHalf = new Area(toDivide.X, toDivide.Y, toDivide.W / 2, toDivide.H);
        
        // Compute the right half of the area.
        Area rightHalf = new Area(toDivide.X + (toDivide.W / 2), toDivide.Y, toDivide.W / 2, toDivide.H);
        
        // Assign the appropriate halved area to each new child node.
        newLChild.Data = leftHalf;
        newRChild.Data = rightHalf;
    }

    private void HalveHorizontally(TreeNode node, TreeNode newLChild, TreeNode newRChild)
    {
        Area toDivide = node.Data;
        
        // Compute the left half of the area.
        Area topHalf = new Area(toDivide.X, toDivide.Y, toDivide.W, toDivide.H / 2);
        
        // Compute the right half of the area.
        Area bottomHalf = new Area(toDivide.X, toDivide.Y + (toDivide.H / 2), toDivide.W, toDivide.H / 2);
        
        // Assign the appropriate halved area to each new child node.
        newLChild.Data = topHalf;
        newRChild.Data = bottomHalf;
    }

    public int[,] GenerateMap()
    {
        // Splitting 3 times gets the best results
        for (int i = 0; i < SplitIterations; i++)
            SplitAll();
        
        // Generate rooms within the bounds of the partitioned areas
        CreateRooms(_leafNodes);
        
        // Generate corridors to attempt to sequentially connect the rooms
        CreateCorridors(_roomList);
        
        // Convert the rooms and corridors from a list of rooms' XYWH into a map with 0 for wall and 1 for floor
        int[,] map = MakeMapArr(_roomList);
        
        // TODO: Use a simple pathfinding algorithm to find unreachable areas
        HandleUnreachableAreas(map, _roomList);
        // Either add teleporters or corridors or something else entirely

        // TODO: Compute some good spots for mobs and items to be spawned, and flag them as 2 and 3?

        return map;
    }

    // Returns a spot with a floor tile, where a player, mob, or item could be placed.
    // Currently returns a 2-element array. A vector would be preferable, but this 
    // needs to work outside the editor.
    public int[] GetEntitySpot()
    {
        Area room = _roomList[_random.Next(_roomList.Count)];
        int x = _random.Next(room.X, room.X + room.W);
        int y = _random.Next(room.Y, room.Y + room.H);
        int[] arr = new int[2];
        arr[0] = x;
        arr[1] = y;
        return arr;
    }
    
    #if !UNITY_EDITOR
    /*
     * The main method is not used in the game itself, but is used for testing
     * the dungeon generation algorithm in a separate executable, compiled with
     * dungeontest.sh.
     */
    public static void Main(string[] args)
    {
        Tree tree = new Tree();
        tree.PrintMapArr(tree.GenerateMap());
    }
    #endif
}
