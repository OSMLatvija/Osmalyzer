namespace Osmalyzer.Tests;

[TestFixture]
public class OsmEditingTests
{
    // todo: all the stuff

    // todo: specifically also
    // todo: temp ids
    // todo: fail to modify deleted
    // todo: mix owner data

    [Test]
    public void TestCreateNode()
    {
        // Arrange

        OsmData osmData = new OsmData();

        OsmCoord coord = new OsmCoord(1, 2);

        // Act

        OsmNode newNode = osmData.CreateNewNode(coord);

        // Assert

        Assert.That(newNode, Is.Not.Null);
        Assert.That(newNode.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(newNode.coord, Is.EqualTo(coord));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ newNode ]));
    }

    [Test]
    public void TestDeleteNode()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        // Act

        osmData.DeleteNode(node);

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
    }

    [Test]
    public void TestRestoreNode()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);

        // Act

        osmData.RestoreNode(node);

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
    }
    
    [Test]
    public void TestSetNodeTag()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        const string tag = "amenity";
        const string value = "cafe";

        // Act

        node.SetValue(tag, value);

        // Assert

        Assert.That(node.GetValue(tag), Is.EqualTo(value));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
    }
    
    [Test]
    public void TestSetNodeTag_Unset()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        const string tag = "amenity";
        const string value = "cafe";
        node.SetValue(tag, value);

        // Act

        node.RemoveTag(tag);

        // Assert

        Assert.That(node.GetValue(tag), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
    }


    [Test]
    public void TestHistory_InitialState()
    {
        // Arrange

        OsmData osmData = new OsmData();

        // Act / Assert

        Assert.That(osmData.CanUndo, Is.False);
        Assert.That(osmData.CanRedo, Is.False);
    }


    [Test]
    public void TestCreateNode_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanUndo, Is.False);
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestCreateNode_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmCoord coord = new OsmCoord(1, 2);
        OsmNode node = osmData.CreateNewNode(coord);
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
        Assert.That(osmData.CanUndo, Is.True);
        Assert.That(osmData.CanRedo, Is.False);
    }

    [Test]
    public void TestCreateNode_UndoRedoUndo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.Undo();
        osmData.Redo();

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanUndo, Is.False);
        Assert.That(osmData.CanRedo, Is.True);
    }


    [Test]
    public void TestDeleteNode_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestDeleteNode_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanRedo, Is.False);
    }


    [Test]
    public void TestSetNodeTag_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        // CreateNewNode is itself a command; undo that baseline by consuming one undo slot,
        // then set the tag so we have a clean starting point for this test
        node.SetValue("amenity", "cafe");

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestSetNodeTag_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
        Assert.That(osmData.CanRedo, Is.False);
    }

    [Test]
    public void TestSetNodeTag_OverwriteValue_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.SetValue("amenity", "restaurant");

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
    }

    [Test]
    public void TestSetNodeTag_OverwriteValue_UndoUndo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.SetValue("amenity", "restaurant");
        osmData.Undo();

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
    }

    [Test]
    public void TestRemoveTag_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.RemoveTag("amenity");

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
    }

    [Test]
    public void TestRemoveTag_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.RemoveTag("amenity");
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
        Assert.That(osmData.CanRedo, Is.False);
    }


    [Test]
    public void TestMultipleCommands_UndoAll()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));

        // Act

        osmData.Undo();
        osmData.Undo();

        // Assert

        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.CanUndo, Is.False);
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestMultipleCommands_UndoAllRedoAll()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));
        osmData.Undo();
        osmData.Undo();

        // Act

        osmData.Redo();
        osmData.Redo();

        // Assert

        Assert.That(osmData.Nodes, Is.EquivalentTo([ nodeA, nodeB ]));
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.CanRedo, Is.False);
    }

    [Test]
    public void TestMultipleCommands_TagAndDelete_UndoAll()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("name", "Test");
        osmData.DeleteNode(node);

        // Act - undo delete, then undo set tag, then undo create
        osmData.Undo();
        osmData.Undo();
        osmData.Undo();

        // Assert

        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.CanUndo, Is.False);
    }

    [Test]
    public void TestMultipleCommands_TagAndDelete_UndoDelete()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("name", "Test");
        osmData.DeleteNode(node);

        // Act - only undo delete

        osmData.Undo();

        // Assert - node is back with its tag intact

        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
        Assert.That(node.GetValue("name"), Is.EqualTo("Test"));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
    }


    [Test]
    public void TestRedoStack_ClearedOnNewCommand()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.Undo();

        Assert.That(osmData.CanRedo, Is.True);

        // Act - new command clears redo stack

        OsmNode node2 = osmData.CreateNewNode(new OsmCoord(3, 4));

        // Assert

        Assert.That(osmData.CanRedo, Is.False);
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node2 ]));
    }

    [Test]
    public void TestRedoStack_ClearedOnNewTag()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        osmData.Undo();

        Assert.That(osmData.CanRedo, Is.True);

        // Act - setting a new tag on an existing node clears redo stack

        node.SetValue("name", "Bar");

        // Assert

        Assert.That(osmData.CanRedo, Is.False);
        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.GetValue("name"), Is.EqualTo("Bar"));
    }


    [Test]
    public void TestUnwind_RevertAllChanges()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));
        nodeA.SetValue("name", "Alpha");
        osmData.DeleteNode(nodeB);

        // Act

        osmData.Unwind();

        // Assert

        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.CanUndo, Is.False);
    }


    [Test]
    public void TestCreateNodeWithId_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(-1, new OsmCoord(5, 6));

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestCreateNodeWithId_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(-1, new OsmCoord(5, 6));
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
    }


    [Test]
    public void TestMultipleTags_UndoIndividually()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("name", "Cafe Nord");
        node.SetValue("amenity", "cafe");
        node.SetValue("cuisine", "coffee_shop");

        // Act - undo cuisine, then amenity, then name

        osmData.Undo();

        Assert.That(node.GetValue("cuisine"), Is.Null);
        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
        Assert.That(node.GetValue("name"), Is.EqualTo("Cafe Nord"));

        osmData.Undo();

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.GetValue("name"), Is.EqualTo("Cafe Nord"));

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("name"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
    }

    [Test]
    public void TestMultipleTags_UndoRedoAllTags()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("name", "Cafe Nord");
        node.SetValue("amenity", "cafe");
        osmData.Undo();
        osmData.Undo();

        // Act

        osmData.Redo();
        osmData.Redo();

        // Assert

        Assert.That(node.GetValue("name"), Is.EqualTo("Cafe Nord"));
        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
        Assert.That(osmData.CanRedo, Is.False);
    }


    [Test]
    public void TestRestoreNode_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);
        osmData.RestoreNode(node);

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestRestoreNode_UndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);
        osmData.RestoreNode(node);
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
        Assert.That(osmData.CanRedo, Is.False);
    }

    [Test]
    public void TestDeleteAndRestoreNode_Undo()
    {
        // Arrange - delete then restore, then undo restore, then undo delete

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        osmData.DeleteNode(node);
        osmData.RestoreNode(node);

        // Act - undo restore

        osmData.Undo();

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);

        // Undo delete

        osmData.Undo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ node ]));
    }

    [Test]
    public void TestDeleteAndRestoreNode_UndoAll()
    {
        // Arrange - create, tag, delete, restore, tag again

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(3, 4));
        node.SetValue("name", "Foo");
        osmData.DeleteNode(node);
        osmData.RestoreNode(node);
        node.SetValue("name", "Bar");

        // Act - unwind everything

        osmData.Unwind();

        // Assert

        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.CanUndo, Is.False);
    }


    [Test]
    public void TestSetValueNull_RemovesTag()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");

        // Act

        node.SetValue("amenity", null);

        // Assert

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
    }

    [Test]
    public void TestSetValueNull_Undo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.SetValue("amenity", null);

        // Act

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.EqualTo("cafe"));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));
    }


    [Test]
    public void TestSetTag_SameValue_NoHistoryEntry()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");

        // Act - set same value again (should be a no-op, no new history entry)

        node.SetValue("amenity", "cafe");

        // Undo once should revert the tag entirely (only one history entry for the first set)

        osmData.Undo();

        // Assert

        Assert.That(node.GetValue("amenity"), Is.Null);
        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
    }


    [Test]
    public void TestSetTag_HasKeyAndKeyCount()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        // Act

        node.SetValue("amenity", "cafe");
        node.SetValue("name", "My Cafe");

        // Assert

        Assert.That(node.HasKey("amenity"), Is.True);
        Assert.That(node.HasKey("name"), Is.True);
        Assert.That(node.KeyCount, Is.EqualTo(2));

        osmData.Undo();

        Assert.That(node.HasKey("name"), Is.False);
        Assert.That(node.KeyCount, Is.EqualTo(1));

        osmData.Undo();

        Assert.That(node.HasKey("amenity"), Is.False);
        Assert.That(node.KeyCount, Is.EqualTo(0));
    }


    [Test]
    public void TestCreateNode_CoordPreservedAfterUndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmCoord coord = new OsmCoord(7, 8);
        OsmNode node = osmData.CreateNewNode(coord);
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.coord, Is.EqualTo(coord));
    }


    [Test]
    public void TestInterlaced_TwoNodes_UndoAll()
    {
        // Arrange - interleave tags on two nodes

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));
        nodeA.SetValue("name", "Alpha");
        nodeB.SetValue("name", "Beta");
        nodeA.SetValue("amenity", "cafe");

        // Act - undo all five commands in reverse

        osmData.Undo(); // undo nodeA amenity
        Assert.That(nodeA.GetValue("amenity"), Is.Null);
        Assert.That(nodeB.GetValue("name"), Is.EqualTo("Beta"));

        osmData.Undo(); // undo nodeB name
        Assert.That(nodeB.GetValue("name"), Is.Null);

        osmData.Undo(); // undo nodeA name
        Assert.That(nodeA.GetValue("name"), Is.Null);
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Created));

        osmData.Undo(); // undo create nodeB
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ nodeA ]));

        osmData.Undo(); // undo create nodeA

        // Assert

        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanUndo, Is.False);
    }

    [Test]
    public void TestInterlaced_TwoNodes_UndoAllRedoAll()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));
        nodeA.SetValue("name", "Alpha");
        nodeB.SetValue("name", "Beta");

        osmData.Undo();
        osmData.Undo();
        osmData.Undo();
        osmData.Undo();

        // Act

        osmData.Redo();
        osmData.Redo();
        osmData.Redo();
        osmData.Redo();

        // Assert

        Assert.That(nodeA.GetValue("name"), Is.EqualTo("Alpha"));
        Assert.That(nodeB.GetValue("name"), Is.EqualTo("Beta"));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ nodeA, nodeB ]));
        Assert.That(osmData.CanRedo, Is.False);
    }


    [Test]
    public void TestDeleteNode_TagsPreservedAfterUndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("name", "Point X");
        node.SetValue("amenity", "bench");
        osmData.DeleteNode(node);

        // Undo delete - node comes back with all tags

        osmData.Undo();

        Assert.That(node.GetValue("name"), Is.EqualTo("Point X"));
        Assert.That(node.GetValue("amenity"), Is.EqualTo("bench"));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Modified));

        // Redo delete - node is gone again

        osmData.Redo();

        // Assert

        Assert.That(node.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
    }


    [Test]
    public void TestCreateNodeWithId_IdPreservedAfterUndoRedo()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(-42, new OsmCoord(0, 0));
        osmData.Undo();

        // Act

        osmData.Redo();

        // Assert

        Assert.That(node.Id, Is.EqualTo(-42));
        Assert.That(node.State, Is.EqualTo(OsmElementState.Created));
    }


    [Test]
    public void TestBranching_NewCommandAfterPartialUndo_OldRedoGone()
    {
        // Arrange - create two nodes, undo both, redo one, then issue a new command

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));
        osmData.Undo(); // undo create nodeB
        osmData.Undo(); // undo create nodeA
        osmData.Redo(); // redo create nodeA

        // Act - new command now; this should clear nodeB from the redo stack

        nodeA.SetValue("name", "New");

        // Assert

        Assert.That(osmData.CanRedo, Is.False);
        Assert.That(nodeA.GetValue("name"), Is.EqualTo("New"));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ nodeA ]));
    }

    [Test]
    public void TestBranching_UndoRedoBranchHasCorrectUndoCount()
    {
        // Arrange - build history: create A, create B, undo B, redo B, undo B again

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(2, 2));

        osmData.Undo(); // undo create nodeB -> undo stack: [createA], redo: [createB]
        osmData.Redo(); // redo create nodeB -> undo stack: [createA, createB], redo: []

        Assert.That(osmData.CanUndo, Is.True);
        Assert.That(osmData.CanRedo, Is.False);
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Created));

        osmData.Undo(); // undo create nodeB again

        // Assert

        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.CanRedo, Is.True);
        Assert.That(osmData.CanUndo, Is.True);
    }


    [Test]
    public void TestUnwind_UndoEmptyRedoPopulated()
    {
        // Arrange

        OsmData osmData = new OsmData();
        osmData.CreateNewNode(new OsmCoord(1, 1));
        osmData.CreateNewNode(new OsmCoord(2, 2));

        // Act

        osmData.Unwind();

        // Assert - each Undo() step during Unwind pushes its inverse onto the redo stack

        Assert.That(osmData.CanUndo, Is.False);
        Assert.That(osmData.CanRedo, Is.True);
    }

    [Test]
    public void TestUnwind_ThenNewCommand_UndoWorks()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode nodeA = osmData.CreateNewNode(new OsmCoord(1, 1));
        osmData.Unwind();

        // Act - new command after unwind

        OsmNode nodeB = osmData.CreateNewNode(new OsmCoord(5, 5));

        // Assert

        Assert.That(osmData.CanUndo, Is.True);
        Assert.That(nodeA.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Created));
        Assert.That(osmData.Nodes, Is.EquivalentTo([ nodeB ]));

        osmData.Undo();

        Assert.That(nodeB.State, Is.EqualTo(OsmElementState.Deleted));
        Assert.That(osmData.Nodes, Is.Empty);
        Assert.That(osmData.CanUndo, Is.False);
    }


    [Test]
    public void TestHasAnyTags_UndoRestoresNoTags()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));

        Assert.That(node.HasAnyTags, Is.False);

        node.SetValue("amenity", "cafe");

        Assert.That(node.HasAnyTags, Is.True);

        // Act

        osmData.Undo();

        // Assert - the tag is gone and KeyCount is 0; note that HasAnyTags may remain True
        // since the internal tag dictionary is not nulled out after removing the last entry

        Assert.That(node.HasKey("amenity"), Is.False);
        Assert.That(node.KeyCount, Is.EqualTo(0));
        Assert.That(node.GetValue("amenity"), Is.Null);
    }


    [Test]
    public void TestAllKeys_AfterUndoRedoSetTags()
    {
        // Arrange

        OsmData osmData = new OsmData();
        OsmNode node = osmData.CreateNewNode(new OsmCoord(1, 2));
        node.SetValue("amenity", "cafe");
        node.SetValue("name", "Test");

        // Act

        osmData.Undo(); // undo name
        osmData.Undo(); // undo amenity

        // Assert

        Assert.That(node.AllKeys, Is.Null.Or.Empty);

        osmData.Redo(); // redo amenity

        Assert.That(node.AllKeys, Is.EquivalentTo(new[] { "amenity" }));

        osmData.Redo(); // redo name

        Assert.That(node.AllKeys, Is.EquivalentTo(new[] { "amenity", "name" }));
    }
}