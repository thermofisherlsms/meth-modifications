# 2.1 Method Modifications

This readme is associated with the latest [2.1 XSD](https://github.com/thermofisherlsms/meth-modifications/blob/master/xsds/Calcium/2.1/MethodModifications.xsd) schema and describes all the possible modifications you can make to 2.1 methods. This readme will focus on some of the new concepts introduced in 2.1. 

Please see the new [2.1 examples](https://github.com/thermofisherlsms/meth-modifications/tree/master/examples/Fusion/2.1) for complete examples.


## Overview

In Tune 2.1, the places where filters could be placed was greatly expanded. It is now possible to assign filters specific to a single outcome that don't affect its siblings. To support this with the XML modifications, being able to select the target node became important. We have added the ability to select any arbitrary node (and its target filters/triggers) using the new XML element: [SourceNodePosition](#Source-Node-Position)

Additionally, some parameters of the scan nodes can now be modified via XML. This is accomplished with the new XML element: [ScanNode](#Modifying-Scan-Parameters). Here you can change certain parameters of generated scan nodes that are modified during XML creation.

## New Features

### Source Node Position

The source node position XML element ([SourceNodePosition](https://github.com/thermofisherlsms/meth-modifications/blob/master/xsds/Calcium/2.1/MethodModifications.xsd#L163)) encompasses a single integer value (0-based) and indicates which child of the currently selected node (left-to-right) to select next. The currently selected node starts as the root node of the experiment, but is updated whenever a SourceNodePosition element is added.

#### Nomenclature

For the following examples, this diagram represents an experiment in a TNG Method. Each letter is a scan node and the lines (| -) represent their connections:

    Example 1:    
        A
        |
        B
        |
        C
         
The above diagram represents a simple MS3 experiment, with 'A' is the OT master scan, 'B' is a MS2, and 'C' is a MS3.

---

    Example 2:  
        A
        |
        B
        |
      -----
      |   |
      C   D
      
 The above diagram represents a slightly more complex MS3 experiment with 2 MS3 scans ('C' and 'D'). Below lists their index (0-based) relative to their direct parent:
 
     Example 3:  
          A     <-- is the default selected node
          |
          B(0)
          |
      ---------
      |       |
      C(0)    D(1)
      
 To select node 'D' from this experiment, your XML would look like this
 
 ```xml
 <!-- rememeber, the currently selected node always defaults to the root node (A) -->
 <SourceNodePosition>0</SourceNodePosition> <!-- Select the first child of the currently selected node, this case node B -->
 <SourceNodePosition>1</SourceNodePosition> <!-- Select the second child of the currently selected node, this case node D -->
 ```
 
 ---
 
 In addition to scan nodes, filters (indicated by asterisks) can now be place below and/or above a scan node.
      
    Example 4:
        A
        |
        B
        |
        *  <-- a filter common to both C and D (it is 'below' B)
        |
      -----
      |   |
      |   *  <-- a filter specific to only D, and not C (it is 'above' D)
      C   D
      
       
#### Examples

To modify the contents of the filter above scan node 'D' for example 4, the following would be done in XML:

```xml
<Modification Order="1">                                                      
  <Experiment ExperimentIndex="0"> 
    <MassListFilter Above="true" MassListType="TargetedMassInclusion"> <!-- we are selecting the node 'Above' our target node (D), so we need to indicate this here -->
      <SourceNodePosition>0</SourceNodePosition> <!-- Select the first child (0-based) of the root node (A). This would be node B -->
      <SourceNodePosition>1</SourceNodePosition> <!-- Select the second child of node B (now the currently selected node). This would be node D -->
      <MassList>   <!-- grab the mass list part of the filter and add the records as you would before -->
        <MassListRecord>    
          <MOverZ>195.12</MOverZ> 
          <Z>1</Z>					
        </MassListRecord>
      </MassList>
    </MassListFilter>
  </Experiment>
</Modification>                                    
```

### Modifying Scan Parameters

Modifying scan nodes involve the same [selection mechanism](#Source-Node-Position) for changing specific filters. So you first select the scan node you want to modify. To select node 'D' from Example 2, you would do the following:

```xml 
<ScanNode> 
    <SourceNodePosition>0</SourceNodePosition> 
	<SourceNodePosition>1</SourceNodePosition> 	
</ScanNode>
```

After the node is selected, you access the [ScanParameters](https://github.com/thermofisherlsms/meth-modifications/blob/master/xsds/Calcium/2.1/MethodModifications.xsd#L141) element and modify the desired settings:

```xml 
<ScanNode> 
    <SourceNodePosition>0</SourceNodePosition> 
	<SourceNodePosition>1</SourceNodePosition> 	
    <ScanParameters>
		<CollisionEnergyCID>25.1</CollisionEnergyCID>
	</ScanParameters>
</ScanNode>
```

## Summary

Tune 2.1 enables a lot more flexibility in method design and updates to the XML modification schema help you interact with the new features it provides. The above features allow selection of any filter or node in a method, no matter how complex. Combined with previous features of XML modification, making complex methods is as simple as writing some XML.