using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif
using Random = System.Random;

public class Tree
{
    private TreeNode _root;
    private List<TreeNode> _leafNodes;

    public const int DungeonWidth   = 80;
    public const int DungeonHeight  = 40;
    public const int SplitMargin    = 1;
    public const float SplitMinRatio= 0.35f;
    public const float SplitMaxRatio= 0.65f;

    private const int RoomMinWidth  = 3;
    private const int RoomMinHeight = 3;

    private const int CorridorWidth = 1;

    private Random _random = new Random();
    
    public Tree()
    {
        _root = new TreeNode(null);
        _root.Data = new Area(0, 0, DungeonWidth, DungeonHeight);
        _leafNodes = new List<TreeNode>();
        _leafNodes.Add(_root);
    }

    public TreeNode GetRoot()
    {
        return _root;
    }
    
    public void SetRoot(TreeNode root)
    {
        _root = root;
    }

    public List<TreeNode> GetLeafNodes()
    {
        return _leafNodes;
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
        bool splitVertically = _random.Next(2) == 1;
        if (splitVertically)
        {
            HalveVertically(node, newLChild, newRChild);
        }
        else
        {
            HalveHorizontally(node, newLChild, newRChild);
        }   
    }

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
    
    // Prints the incomplete version of the map, which indicates the numbered binary partitions.
    public void PrintPartitionsMap()
    {
        int[,] mapArr = new int[DungeonWidth, DungeonHeight];

        Console.WriteLine("Size of arr: " + mapArr.Length);
        int numLeafNodes = _leafNodes.Count;

        TreeNode currentNode;
        Area currentArea;
        
        for (int i = 0; i < numLeafNodes; i++)
        {
            currentNode = _leafNodes[i];
            currentArea = currentNode.Data;

            for (int y = 0; y < currentArea.H; y++)
            {
                for (int x = 0; x < currentArea.W; x++)
                {
                    mapArr[currentArea.X + x, currentArea.Y + y] = i;
                }
            }
        }

        PrintMapArr(mapArr);
    }
    
    // Takes a fully partitioned list of nodes with areas and put a room in each one.
    // Returns a list of Areas describing rooms. Essentially randomly carves out a room within
    // the bounds of each Area given by the Data members of nodes in the node list.
    public List<Area> CreateRooms(List<TreeNode> leafNodesList)
    {
        List<Area> roomList = new List<Area>();

        TreeNode currentNode;
        Area currentArea;
        // For each node in the node list:
        for (int i = 0; i < leafNodesList.Count; i++)
        {
            // Get current Area.
            currentNode = leafNodesList[i];
            currentArea = currentNode.Data;
            
            // Pick a random x and y offset, both within width/2 and height/2.
            int xOffset = _random.Next(1, currentArea.W / 2);
            int yOffset = _random.Next(1, currentArea.H / 2);
            
            // Determine right and upper bounds based on the selected room offsets
            int widthBound = currentArea.W - xOffset;
            int heightBound = currentArea.H - yOffset;
            
            // Select width and height within the bounds
            int roomWidth = _random.Next((int) (widthBound * SplitMinRatio), widthBound);
            int roomHeight = _random.Next((int) (heightBound * SplitMinRatio), heightBound);

            Area newRoom = new Area(
                currentArea.X + xOffset,
                currentArea.Y + yOffset,
                roomWidth,
                roomHeight);
            roomList.Add(newRoom);
            
        }
        return roomList;
    }
    // Takes a list of rooms and adds to the list a series of corridors between rooms.
    public List<Area> CreateCorridors(List<Area> roomList)
    {
        // Lazy algorithm: Iterates over the list, connecting A to B, B to C, and C to D.
        Area roomA, roomB;
        Area pointA, pointB;
        List<Area> corridors = new List<Area>(); // added at once after all are generated.

        for (int i = 0; i < roomList.Count - 1; i++)
        {
            roomA = roomList[i];
            roomB = roomList[i + 1];

            pointA = RandPointWithin(roomA);
            pointB = RandPointWithin(roomB);
            
            // Create a rectangular area that spans the width.
            Area widthSpan;
            int corridorWidth;
            // If A is to the left of B
            if (pointA.X < pointB.X)
            {
                corridorWidth = pointB.X - pointA.X;
                widthSpan = new Area(pointA.X, pointA.Y, corridorWidth, CorridorWidth);
            }
            // If B is to the left of A
            else //if (pointB.X < pointA.X)
            {
                corridorWidth = pointA.X - pointB.X;
                widthSpan = new Area(pointB.X, pointB.Y, corridorWidth, CorridorWidth);
            }
            corridors.Add(widthSpan);
            
            // Create a rectangular area that spans the height.
            Area heightSpan;
            int corridorHeight;
            // If A is above B
            if (pointA.Y < pointB.Y)
            {
                corridorHeight = pointB.Y - pointA.Y;
                heightSpan = new Area(pointA.X , pointA.Y, CorridorWidth, corridorHeight);
            }
            // If B is above A
            else
            {
                corridorHeight = pointA.Y - pointB.Y;
                heightSpan = new Area(pointB.X, pointB.Y, CorridorWidth, corridorHeight);
            }
            corridors.Add(heightSpan);
        }

        foreach (Area corridor in corridors)
            roomList.Add(corridor);
        
        return roomList;
    }

    private Area RandPointWithin(Area room)
    {
        Area point = new Area(0, 0, 0, 0);
        point.X = _random.Next(room.X, room.X + room.W);
        point.Y = _random.Next(room.Y, room.Y + room.H);
        return point;
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
        // Generate rooms within the bounds of the partitioned areas
        List<Area> roomList = CreateRooms(_leafNodes);
        
        // Generate corridors to attempt to sequentially connect the rooms
        roomList = CreateCorridors(roomList);
        
        // Convert the rooms and corridors from a list of rooms' XYWH into a map with 0 for wall and 1 for floor
        int[,] map = MakeMapArr(roomList);
        
        // TODO: Use a simple pathfinding algorithm to find unreachable areas
        // Either add teleporters or corridors or something else entirely
        
        // TODO: Compute some good spots for mobs and items to be spawned, and flag them as 2 and 3?

        return map;
    }
    
    private static void PrintGoodStuff(List<TreeNode> nodeList)
    {
        Console.WriteLine("========= Current Leaf Nodes ==============");
        foreach (TreeNode node in nodeList)
        {
            Console.Write("x = " + node.Data.X);
            Console.Write("y = " + node.Data.Y);
            Console.Write("w = " + node.Data.W);
            Console.WriteLine("h = " + node.Data.H);
        }
    }
    public static void Main(string[] args)
    {
        Tree tree = new Tree();
        var li1 = tree.GetLeafNodes();
        PrintGoodStuff(li1);
        
        tree.SplitAll();
        var li2 = tree.GetLeafNodes();
        PrintGoodStuff(li2);
        
        tree.SplitAll();
        var li3 = tree.GetLeafNodes();
        PrintGoodStuff(li3);
        
        tree.SplitAll();
        var li4 = tree.GetLeafNodes();
        PrintGoodStuff(li4);

        Console.WriteLine("......");
        tree.PrintMapArr(tree.MakeMapArr(tree.CreateCorridors(tree.CreateRooms(tree.GetLeafNodes()))));
    }
}
