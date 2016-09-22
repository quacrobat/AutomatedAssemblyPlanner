;



/**
*
* Accepts an array of objects containing a string property called "Name" and returns
* the first index where any two "Name" properties in the array have different characters.
*
*
* @method getPartNameCutoff
* @for renderGlobal
* @param {Object Array} partFrames An array of objects, each of which should containin
* a property called "Name" with a non-null string.
* @return {Int} The first index where any two "Name" properties in the array are different.
* 
*/
function getPartNameCutoff(partFrames){
	
	var pos=1;
	var lim=partFrames.length;
	var checkPos;
	var checkLim=partFrames[0].Name.length;
	while(pos<lim){
		checkPos=0;
		while(checkPos<checkLim){
			if(partFrames[pos].Name[checkPos]!=partFrames[0].Name[checkPos]){
				checkLim=checkPos;
			}
			checkPos++;
		}
		pos++;
	}
	
	return checkLim;
	
}


/**
*
* Accepts an array of objects containing a string property called "Name" removes
* the first N characters in each string, where N is the first index where any two
* "Name" properties in the array have different characters.
*
*
* @method cutoffPartNames
* @for renderGlobal
* @param {Object Array} partFrames An array of objects, each of which should containin
* a property called "Name" with a non-null string.
* @return {Void}
* 
*/
function cutoffPartNames(partFrames){
	
	var cut=getPartNameCutoff(partFrames);
	
	var pos=0;
	var lim=partFrames.length;
	while(pos<lim){
		partFrames[pos].Name=partFrames[pos].Name.substr(cut,partFrames[pos].Name.length-(cut+4));
		pos++;
	}
	
}


/**
*
* Finds the average position of all the vertices in a given threeJS Geometry model.
*
* @method centerGeometry
* @for renderGlobal
* @param {threeJS Geometry Object} theGeo
* @return {threeJS Vector3 Object} A 3d coordinate, with each component being the unweighted
* average of the corresponding component in each vector in the provided geometry object. If nodeName
* vertices are present in the object, a zeroed vector is returned.
* 
*/
function centerGeometry(theGeo){
	
	var verts=theGeo.vertices;
	var pos=0;
	var lim=verts.length;
	var avgX=0;
	var avgY=0;
	var avgZ=0;
	if(lim==0){
		return new THREE.Vector3(avgX,avgY,avgZ);
	}
	while(pos<lim){
		avgX+=verts[pos].x;
		avgY+=verts[pos].y;
		avgZ+=verts[pos].z;
		pos++;
	}
	
	
	
	avgX/=lim;
	avgY/=lim;
	avgZ/=lim;
	pos=0;
	while(pos<lim){
		verts[pos].x-=avgX;
		verts[pos].y-=avgY;
		verts[pos].z-=avgZ;
		pos++;
	}
	
	return new THREE.Vector3(avgX,avgY,avgZ);
	
}




function getGeometries(theSTLs){
	
	var result=[];
	var pos=0;
	var lim=theSTLs.length;
	while(pos<lim){
		result.push(parseStlBinary(theSTLs[pos]));
		pos++;
	}
	return result;
	
}




/**
*
* Combines a given array of objects (each associating an array of keyframes to name) with a 
* given array of objects (each associating a threeJS mesh with a name), creating an array
* of objects with keyFrame arrays and threeJS meshes associated with the same name
*
* @method bindPartsToKeyFrames
* @for renderGlobal
* @param {Array} theKeyFrameLists An array of objects, each containing an array of keyframe objects 
* called "Frames" and a string property called "Name")
* @param {Array} theParts An array of objects, each containing a threeJS mesh object called "Mesh" and
* a string property called "Name"
* @return {Array} 
*
*/
function bindPartsToKeyFrames(theKeyFrameLists, theParts){
	
	console.log(theKeyFrameLists);
	console.log (theParts);
	
	var pos=0;
	var searchPos;
	var lim=theKeyFrameLists.length;
	var searchLim=theParts.length;
	var result=[];
	
	
	
	while(pos<lim){
		searchPos=0;
		while(searchPos<searchLim){
			if(theKeyFrameLists[pos].Name===theParts[searchPos].Name+".STL"){
				break;
			}
			//console.log(theKeyFrameLists[pos].Name);
			//console.log(theParts[searchPos].Name);
			searchPos++;
		}
		if(searchPos==searchLim){
			pos++;
			continue;
		}
		result.push({
			Name: theKeyFrameLists[pos].Name,
			Frames: theKeyFrameLists[pos].Frames,
			Mesh: theParts[searchPos].Mesh
		});
		console.log({
			Name: theKeyFrameLists[pos].Name,
			Frames: theKeyFrameLists[pos].Frames,
			Mesh: theParts[searchPos].Mesh
		});
		pos++;
	}
	
	flipTheTimes(result);
	console.log(result);
	
	console.log("AAAAAARG");
	return result;
	
}



/**
*
* Combines a jagged array of objects, each object at least possessing a numeric
* property called "Time", returns the value of the greatest "Time" property
*
* @method longestTimeFromFrames
* @for renderGlobal
* @param {Array} partFrames The jagged array
* @return {Int} The greatest "Time" value in the jagged array
*
*/
function longestTimeFromFrames(partFrames){
	
	var x=0;
	var y;
	var xLim=partFrames.length;
	var yLim;
	var best=0;
	while(x<xLim){
		y=0;
		yLim=partFrames[x].Frames.length;
		while(y<yLim){
			best=Math.max(best,partFrames[x].Frames[y].Time);
			y++;
		}
		x++;
	}
	//console.log(best);
	return best;
	
}


/**
*
* Given a jagged array of objects, each object at least possessing a numeric
* property called "Time", sets each Time property to the greatest Time value in
* the jagged array minus the origional value, thus effectively reversing the
* temporal order of each object
*
* @method flipTheTimes
* @for renderGlobal
* @param {Array} partFrames The jagged array
* @return {Int} The greatest "Time" value in the jagged array
*
*/
function flipTheTimes(partFrames){
	
	var x=0;
	var y;
	var xLim=partFrames.length;
	var yLim;
	var tFlip=longestTimeFromFrames(partFrames);
	while(x<xLim){
		y=0;
		yLim=partFrames[x].Frames.length;
		while(y<yLim){
			partFrames[x].Frames[y].Time=tFlip-partFrames[x].Frames[y].Time;
			y++;
		}
		partFrames[x].Frames.reverse();
		x++;
	}
	return;
	
}


/**
*
* Logs the contents of the given jagged array of keyFrame objects, each containing numeric properties "X",
* "Y", "Z", and "Time", to the console as a string.
*
* @method showFrames
* @for renderGlobal
* @param {Array} partFrames A jagged array of keyframe objects
* @return {Void}
*
*/
function showFrames(partFrames){
	
	var x=0;
	var y;
	var xLim=partFrames.length;
	var yLim;
	var tFlip=longestTimeFromFrames(partFrames);
	var timeList;
	var listList=[];
	while(x<xLim){
		timeList=[];
		y=0;
		yLim=partFrames[x].Frames.length;
		while(y<yLim){
			theFrame=partFrames[x].Frames[y];
			timeList.push("X: "+theFrame.Position.x+" Y: "+theFrame.Position.y+" Z: "+theFrame.Position.z+" Time: "+theFrame.Time);
			y++;
		}
		listList.push(timeList);
		x++;
	}
	console.log(listList);
	return;
	
}


/**
*
* Logs the contents of a given keyFrame object, containing numeric properties "X",
* "Y", "Z", and "Time", to the console as a string.
*
* @method showFrame
* @for renderGlobal
* @param {Array} theFrame the keyFrame object to be logged
* @return {Void}
*
*/
function sayFrame(theFrame){
	
	console.log("X: "+theFrame.Position.x+" Y: "+theFrame.Position.y+" Z: "+theFrame.Position.z+" Time: "+theFrame.Time);
	
}


/**
*
* Returns true if any position component of the given keyframe object is NaN
*
* @method hasNaN
* @for renderGlobal
* @param {Object} theFrame A keyFrame object
* @return {Boolean}
*
*/
function hasNaN(theFrame){
	
	return (isNaN(theFrame.Position.x) || isNaN(theFrame.Position.y) || isNaN(theFrame.Position.z));
	
}


/**
*
* Returns a copy of the provided keyframe object
*
* @method copyFrame
* @for renderGlobal
* @param {Object} theFrame A keyFrame object
* @return {Object} The copy
*
*/
function copyFrame(theFrame){
	var result={
		Quat: new THREE.Quaternion(0,0,0,0),
		Position: null,
		Time: null
	}
	result.Quat.copy(theFrame.Quat);
	result.Position=theFrame.Position.clone();
	result.Time=theFrame.Time;

	return result;
}


/**
*
* Returns a copy of the provided array of keyframe objects
*
* @method copyFrameList
* @for renderGlobal
* @param {Array} partFrames A keyFrame object array
* @return {Array} The copy
*
*/
function copyFrameList (theFrameList){
	
	var pos=0;
	var lim=theFrameList.length;
	var result=[];
	while(pos<lim){		
		result.push(copyFrame(theFrameList[pos]));
		pos++;
	}
	
	return result;
	
}


/**
*
* Given a tree representation of the assembly process through nested javascript objects, returns an array
* of keyframe array objects, with each keyframe array object being a representation of the movement of each part 
* in the tree representation throughout the assembly proceess, with a list of keyframe objects and a given part name
*
* @method makeKeyFrames
* @for renderGlobal
* @param {Object} theTree Tree representation of the assembly process through nested javascript objects
* @param {Array} runningList Internally used variable. Should be an empty array for outside use.
* @param {Array} currentFrameList Internally used variable. Should be an empty array for outside use.
* @return {Array} The jagged array of keyFrame objects
*
*/
function makeKeyFrames(theTree, runningList, currentFrameList){

	var isRoot=0;
	if(runningList.length==0){
		isRoot=1;
		console.log("ROOT");
		console.log(theTree);
	}
	
	console.log(theTree);
	
	var newQuat= new THREE.Quaternion();
	newQuat.setFromEuler(new THREE.Euler(0,0,0,'XYZ'));
	
	var presentFrame={
						Quat: newQuat, 
						Position: new THREE.Vector3(theTree.X,theTree.Y,theTree.Z),
						Time: theTree.Time
					};

	
	runningList.push(presentFrame);
	
	if(theTree.Ref===null){
		var copiedList= copyFrameList(runningList);
		console.log("-----------");
		currentFrameList.push({Name: theTree.Name, Frames: copiedList});
		runningList.pop();
	}
	else{
		makeKeyFrames(theTree.Ref,runningList,currentFrameList);
		makeKeyFrames(theTree.Mov,runningList,currentFrameList);
		runningList.pop();
	}
	
	if(isRoot===1){
		//console.log(currentFrameList);
		return currentFrameList;
	}
	return;
	
}



/**
*
* Given two keyFrames and a normalized float "proportion", returns an interpolation
* between the two keyframes with a weight towards the second keyframe proportional
* to "proportion"
*
* @method interpolate
* @for renderGlobal
* @param {Object} keyFrame1 The earlier keyFrame
* @param {Object} keyFrame2 The later keyFrame
* @param {Float} proportion A normalized value representing what proportion of the path of
* interpolation is between the result and the earlier keyFrame
* @return {Object} The jagged array of keyFrame objects
*
*/
function interpolate(keyFrame1, keyFrame2, proportion){
	
	var result={ Quat: new THREE.Quaternion(), Position: new THREE.Vector3(0,0,0), Time: null };
	
	THREE.Quaternion.slerp (keyFrame1.Quat, keyFrame2.Quat, result.Quat, proportion);
	//result.Position.lerp(keyFrame1.Position,keyFrame2.Position,proportion);
	result.Position.x=keyFrame1.Position.x*(1-proportion)+keyFrame2.Position.x*proportion;
	result.Position.y=keyFrame1.Position.y*(1-proportion)+keyFrame2.Position.y*proportion;
	result.Position.z=keyFrame1.Position.z*(1-proportion)+keyFrame2.Position.z*proportion;
	result.Time=keyFrame1.Time*(1-proportion)+keyFrame2.Time*proportion;
	
	
	if(hasNaN(result)){
		console.log("vvvvvvvvvvvv");
		console.log("Prop: "+proportion);
		sayFrame(keyFrame1);
		sayFrame(keyFrame2);
		sayFrame(result);
		console.log("^^^^^^^^^^^^");
	}	
	
	return result;
	
}



/**
*
* Given a list of keyframes and a time quantity, returns a keyframe object interpolating
* between the two temporally closest keyframes. In cases where the provide time is beyond the
* range of times represented by the list, returns the closest keyframe
*
* @method grabInterp
* @for renderGlobal
* @param {Array} frameList A list of keyframes. Must be organized from least time value to greatest time value
* @param {Float} time Floating-point representation of what time in the keyframe progression the interpolation
* should occur
* @return {Object} The interpolated keyframe
*
*/
function grabInterp(frameList, time){
	

	var pos=0;
	var lim=frameList.length;
	//var timeReport="";
	while((pos<lim) && (time>frameList[pos].Time)){
		//timeReport=timeReport+" -> "+frameList[pos].Time.toString();
		pos++; 
	}
	
	
	/*if(pos<lim){
		timeReport=timeReport+" -> "+time+" <- "+frameList[pos].Time.toString();
	}
	else{
		timeReport=timeReport+" -> "+frameList[lim-1].Time.toString()+" -> "+time;
	}
	console.log(timeReport);*/
	
	if(pos==0){
		//console.log(time.toString()+"<"+frameList[0].Time.toString());
		var theResult= copyFrame(frameList[0]);
		
	}
	else if(pos==lim){
		//console.log(time.toString()+">"+frameList[lim-1].Time.toString());
		var theResult= copyFrame(frameList[lim-1]);
	}
	else{
		
		var prop=(time-frameList[pos-1].Time)/(frameList[pos].Time-frameList[pos-1].Time);
		var theResult=interpolate(frameList[pos-1],frameList[pos],prop);
	}
	
	//sayFrame(theResult);
	return theResult;
	
}



/**
*
* Given an array of objects (each containing a threeJS mesh object and an array of 
* keyFrame objects), and two floating points "time" and "timeWarp", will animate each
* mesh in the array along the keyframes in their associate objects according to the 
* given "time" and returns the new time as given by the standard time step multiplied
* by "timeWarp" 
*
* @method interpolate
* @for renderGlobal
* @param {Array} partFrames List of objects relating threeJS mesh objects with their 
* respective keyframe arrays 
* @param {Float} time The time to be used when interpolating keyFrames for the models
* @param {Float} timeWarp The coefficeint to be applied to the timestep in the
* animation
* @return {Float} The new value of time in the animation
*
*/
function animate(partFrames, time, timeWarp){
	
	var pos=0;
	var lim=partFrames.length;
	var interp;
	var eul= new THREE.Euler();
	var delt=new THREE.Vector3();
	while(pos<lim){
		
		interp=grabInterp(partFrames[pos].Frames,time,partFrames[pos].Mesh.position);
		
		/*delt=partFrames[pos].Mesh.position.sub(interp.Position);
		
		
		if(delt.length()>0.00000001){
			console.log(delt.length());
			console.log(delt);
		}*/
		
		eul.setFromQuaternion(interp.Quat);
		partFrames[pos].Mesh.rotation.x=eul.x;
		partFrames[pos].Mesh.rotation.y=eul.y;
		partFrames[pos].Mesh.rotation.z=eul.z;
		partFrames[pos].Mesh.position.x=interp.Position.x;
		partFrames[pos].Mesh.position.y=interp.Position.y;
		partFrames[pos].Mesh.position.z=interp.Position.z;
		
		

		pos++;
	}
	
	var timeStep=timeWarp/60;
	time+=timeStep;
	return time;
	
}




/**
*
* Given two threeJS boundingBox objects, returns the smallest bounding box
* encompassing the two
*
* @method combineBounds
* @for renderGlobal
* @param {Object} a The first bounding box
* @param {Object} b The second bounding box
* @return {Object} The combined bounds
*
*/
function combineBounds(a,b){
	
	var r={};
	r.min= new THREE.Vector3();
	r.max= new THREE.Vector3();
	r.min.x = Math.min(a.min.x,b.min.x);
	r.max.x= Math.max(a.max.x,b.max.x);
	r.min.y = Math.min(a.min.y,b.min.y);
	r.max.y= Math.max(a.max.y,b.max.y);
	r.min.z = Math.min(a.min.z,b.min.z);
	r.max.z= Math.max(a.max.z,b.max.z);
	return r;
	
}



/**
*
* Given two threeJS boundingBox objects, returns the smallest bounding box
* encompassing the two
*
* @method getGlobBounds
* @for renderGlobal
* @param {Object} a The first bounding box
* @param {Object} b The second bounding box
* @return {Object} The combined bounds
*
*/
function getGlobBounds(partFrames){
	
	
	partFrames[0].Mesh.geometry.computeBoundingBox();
	var runningBound=partFrames[0].Mesh.geometry.boundingBox;
	
	var pos=1;
	var lim=partFrames.length;
	while(pos<lim){
		partFrames[pos].Mesh.geometry.computeBoundingBox();
		runningBound=combineBounds(runningBound,partFrames[pos].Mesh.geometry.boundingBox);
		pos++;
	}
	
	return runningBound;
	
}





/**
*
* Given an object, containing a threeJS mesh object as "Mesh", will
* return the center of the mesh's bounding box
*
* @method getPartCenter
* @for renderGlobal
* @param {Object} a The object containing the threeJS mesh object
* @return {Object} The center of the mesh's bounding box, represented as
* a threeJS Vector3 object
*
*/
function getPartCenter(part){
	
	part.Mesh.geometry.computeBoundingBox();
	var bound=part.Mesh.geometry.boundingBox;
	var center= new THREE.Vector3((bound.min.x+bound.max.x)/2,(bound.min.y+bound.max.y)/2,(bound.min.z+bound.max.z)/2);
	center.x+=part.Mesh.position.x;
	center.y+=part.Mesh.position.y;
	center.z+=part.Mesh.position.z;
	
	return center;
	
}







/**
*
* Given a threeJS scene object, a threeJS camera object, and an array of objects containing
* threeJS mesh objects, finds the first mesh in the scene which is intersected the ray extending
* through the center of the camera's field of vision. If this mesh is in the provided array of 
* objects, then that object is returned, otherwise null is returned instead
*
* @method getFirstIntersect
* @for renderGlobal
* @param {Object} theScene The threeJS scene object in which intersections should
* be tested
* @param {Object} theCamera The threeJS camera object to be used to test for 
* ray intersections
* @param {Array} partFrames An array containing a series of objects, each of which
* contain a threeJS mesh object (under the property "Mesh") to be tested for intersections
* @return {Object} The intersecting mesh (or null in case of no valid intersection)
*
*/
function getFirstIntersect(theScene, theCamera, partFrames){
	
	var caster= new THREE.Raycaster();
	var mousePos= new THREE.Vector2(0,0);
	
	caster.setFromCamera(mousePos,theCamera);
	
	var intersectList=caster.intersectObjects(theScene.children);
	
	if(intersectList.length===0){
		return null;
	}
	else{
		
		var pos=0;
		var lim=partFrames.length;
		var part=intersectList[0].object;
		while(pos<lim){
			if(part===partFrames[pos].Mesh){
				return partFrames[pos];
			}
			/*console.log("vvvvvvvvv");
			console.log(part);
			console.log(partFrames[pos].Mesh);
			console.log("^^^^^^^^^");*/
			pos++;
		}
		return null;		
	}
	
}






/**
*
* Given an tree representation of the movement of parts in an assembly sequence, the
* parent node of that node, and a threeJS scene object, inserts a line for each subassembly
* path along the path of movement
*
* @method addLines
* @for renderGlobal
* @param {Object} movTree Tree of nested objects representing the movement of each subassembly
* in it's assembly sequence
* @param {Object} parentNode Used for internal use. Null should be applied for external use.
* @param {Object} theScene the threeJS scene to which the line representations will be added
* @return {Void}
*
*/
function addLines(movTree,parentNode,theScene){
	
	if(movTree==null){
		return;
	}
	else{
		if(parentNode!=null){
			var theGeo=new THREE.Geometry();
			var startP=new THREE.Vector3(movTree.X,movTree.Y,movTree.Z);
			var endP=new THREE.Vector3(parentNode.X,parentNode.Y,parentNode.Z);
			theGeo.vertices=[startP,endP];

			
			movTree.Line= new THREE.LineSegments(
				theGeo,
				new THREE.LineDashedMaterial({
					color: 0x333333,
					dashSize: 50,
					gapSize:50
				
				})
			);
			theScene.add(movTree.Line);
			
		}
		else{
			movTree.Line= null;
		}
		addLines(movTree.Ref,movTree,theScene);
		addLines(movTree.Mov,movTree,theScene);
		
		return;
	}
	
}




/**
*
* Given an tree representation of the movement of parts in an assembly sequence, an 
* array of Objects each associating a list of keyframes with a threeJS mesh object and aLinkcolor
* string, and the index of the keyframe associated with the tree's root node, displaces the movement
* line points associated with that particular part of the assembly to match the displacement of the
* model
*
* @method addDisplacement
* @for renderGlobal
* @param {Object} movTree Tree of nested objects representing the movement of each subassembly
* in it's assembly sequence
* @param {Array} partFrames An array of Objects each associating a list of keyframes with a threeJS
* mesh object and a string
* @param {Object} it The index of the keyframe associated with the root node of movTree. Used internally.
* For external use, apply 0.
* @return {Void}
*
*/
function addDisplacement(movTree, partFrames, it){
	
	if(movTree==null){
		return;
	}
	else{
		var ref=addDisplacement(movTree.Ref,partFrames,it);
		if(ref!=null){
			var mov=addDisplacement(movTree.Mov, partFrames, ref);
			it=mov;
		}
		
		if(mov==null | ref==null){
			movTree.Disp=getPartCenter(partFrames[it]);
			it++;
		}
		else{
			movTree.Disp=new THREE.Vector3(0,0,0);
			movTree.Disp.lerpVectors(movTree.Ref.Disp,movTree.Mov.Disp,0.5);
			/*movTree.Disp.x=(movTree.Ref.Disp.x+movTree.Mov.Disp.x)/2;
			movTree.Disp.y=(movTree.Ref.Disp.y+movTree.Mov.Disp.y)/2;
			movTree.Disp.z=(movTree.Ref.Disp.z+movTree.Mov.Disp.z)/2;*/
		}
		if(movTree.Line!=null){
			movTree.Line.geometry.vertices[0].addVectors(movTree.Line.geometry.vertices[0],movTree.Disp);
			movTree.Line.geometry.vertices[1].addVectors(movTree.Line.geometry.vertices[1],movTree.Disp);	
		}
		
		return it;
		
	}
	
	
}


/**
*
* Given an tree representation of the movement of parts in an assembly sequence, the
* parent node of that node, and the current time in the animation, updates the ends of the
* movement lines such that the portion of lines which have already been traversed are not shown
*
* @method updateLines
* @for renderGlobal
* @param {Object} movTree Tree of nested objects representing the movement of each subassembly
* in it's assembly sequence
* @param {Object} parentNode Used for internal use. Null should be applied for external use.
* @param {Object} theTime the threeeJS scene to which the line representations will be added
* @return {Void}
*
*/
function updateLines(movTree,parentNode,theTime){
	
	if(movTree==null){
		return;
	}
	else{
		if(movTree.Line!=null && parentNode!=null){
			if(theTime<=parentNode.Time){
				if(theTime>=movTree.Time){
					var normTime=(parentNode.Time-theTime)/(parentNode.Time-movTree.Time);
					movTree.Line.geometry.vertices[0].x=(movTree.X+movTree.Disp.x)*(normTime)+(parentNode.X+movTree.Disp.x)*(1-normTime);
					movTree.Line.geometry.vertices[0].y=(movTree.Y+movTree.Disp.y)*(normTime)+(parentNode.Y+movTree.Disp.y)*(1-normTime);
					movTree.Line.geometry.vertices[0].z=(movTree.Z+movTree.Disp.z)*(normTime)+(parentNode.Z+movTree.Disp.z)*(1-normTime);
					/*console.log("X: "+movTree.Line.geometry.vertices[0].x+
					" Y: "+movTree.Line.geometry.vertices[0].y+
					" Z: "+movTree.Line.geometry.vertices[0].z);*/

				}
				else{
					movTree.Line.geometry.vertices[0].x=movTree.X+movTree.Disp.x;
					movTree.Line.geometry.vertices[0].y=movTree.Y+movTree.Disp.y;
					movTree.Line.geometry.vertices[0].z=movTree.Z+movTree.Disp.z;
				}
			}
			else{
				//console.log("time: "+theTime+" mov: "+movTree.Time+" parent: "+parentNode.Time);
				movTree.Line.geometry.vertices[0]=movTree.Line.geometry.vertices[1].clone();
			}
			movTree.Line.geometry.verticesNeedUpdate=true;
		}
		
		updateLines(movTree.Ref,movTree,theTime);
		updateLines(movTree.Mov,movTree,theTime);
		return;
	}
	
}




