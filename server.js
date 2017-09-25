


// content of index.js
const express = require('express');
const bodyParser = require('body-parser');
const multer = require('multer'); // v1.0.5
const upload = multer();
const handlebars = require('handlebars');
const fs = require('fs');
const exec = require('child_process').exec;
const execSync = require('child_process').execSync;
const app = express();
const port = 3000;
const tempRoute = "templates";
const sessionRoute = "workspace";
const theTimeout = 1000*60*60*24;
var theDate = new Date();

var contentManifest = {

    jquery:"jquery.js",
    threeJS:"three.min.js",
    jsstl:"jsstl.js",
    treequence:"treequence.js",
    partRender:"partRender.js",

    pageBase:"pageBase.html",
    stageBase:"stageBase.html",

    progMain:"progress.html",
    progStyle:"progress.css",
    progScript:"progess.js",

    pageBaseStyle:"pageBaseStyle.css",
    pageBaseScript:"pageBaseScript.js",

    uploadMain:"upload.html",
    uploadStyle:"uploadStyle.css",
    uploadScript:"uploadScript.js",

    partPropMain:"partProp.html",
    partStyle:"partPropStyle.css",
    partScript:"partPropScript.js",

    dirConMain:"dirCon.html",
    dirStyle:"dirConStyle.css",
    dirScript:"dirConScript.js",

    renderMain:"render.html",
    renderStyle:"renderStyle.css",
    renderScript:"renderScript.js"

};

var content = {};

var baseTemplate = handlebars.compile(fs.readFileSync(tempRoute+contentManifest.pageBase));
var stageTemplate = handlebars.compile(fs.readFileSync(tempRoute+contentManifest.pageBase));

var sessions = {};

function killDir(thePath){

    fs.readdir(thePath,
        (function(dirPath){
            return (function(err,theFiles){
                for( f in theFiles ){
                    fs.unlinkSync();
                }
                rmdirSync(dirPath);
            })
        })(thePath)
    );

}

function sweepDir(thePath){

    fs.readdir(thePath,
        (function(dirPath){
            return (function(err,theFiles){
                for( f in theFiles ){
                    fs.unlinkSync();
                }
            })
        })(thePath)
    );

}

function sweepSessions(){

    var theKeys = Object.keys(sessions);
    var rightNow = theDate.now();
    for ( k in theKeys ){
        if(sessions[k].startTime + theTimeout < rightNow){
            killDir(sessions[k].filePath+"/intermediate");
            killDir(sessions[k].filePath+"/models");
            killDir(sessions[k].filePath+"/XML");
            killDir(sessions[k].filePath);
            delete sessions[k];
        }
    }

}

function setupSession(thePath,theModels){

    fs.mkdirSync(thePath);
    fs.mkdirSync(thePath+"/intermediate");
    fs.mkdirSync(thePath+"/models");
    fs.mkdirSync(thePath+"/XML");

    for( p in theModels){
        fs.writeFileSync(thePath+"/models/" + p.Name, p.Data, 'ascii');
    }

}

function getHex( theChar ){

    var hex = "0123456789ABCDEF";
    var bottom = theChar%16;
    var top = (theChar/16)%16;
    return hex[top]+hex[bottom];

}

function makeID(){

    var idLen = 16;
    var array = new Uint8Array(idLen);
    window.crypto.getRandomValues(array);
    var result = "";
    var check1 = 0;
    var check2 = 0;
    var check4 = 0;
    var check8 = 0;

    var idPos = 0;
    while(idPos < idLen){
        if(idPos & 1 != 0){
            check1 += array[idPos];
        }
        if(idPos & 2 != 0){
            check2 += array[idPos];
        }
        if(idPos & 4 != 0){
            check4 += array[idPos];
        }
        if(idPos & 8 != 0){
            check8 += array[idPos];
        }
        result = result + getHex(array[idPos]);
        idPos++;
    }

    result = result + getHex(check1) + getHex(check2) + getHex(check4) + getHex(check8);

}

function makeSession(){

    var theId = makeID();
    while( typeof sessions[theId] != 'undefined'){
        sweepSessions();
        theId = makeID();
    var bodyParser = require('body-parser');
    }
    return {
        filePath: sessionRoute + "/" +theID,
        id: theID,
        startTime: theDate.now(),
        stage: 0,
        state: {
            models: [],
            partsPropertiesIn: "",
            partsPropertiesOut: "",
            dirConfirmIn: "",
            dirConfirmOut: "",
            renderIn:""
        }
    }

}



function basicResponse(response, theID){

    return function(error,stdout,stderr){
        response.json({
            sessID: theID,
            failed: (error === null)
        });
    }

}

function runResponse(exeFile,sessID,textFile,textData){

    return (function(){
            exec(exeFile, sessions[sessID].filePath, "y", "1", "0.5", "y", "y",basicResponse(response,sessID));
    });

}

function execResponse(exeFile,sessID,textFile,textData){

    if(textFile === ""){
        (runResponse(exeFile,sessID,textFile,textData))();
    }
    else{
        writeFile(textFile,textData,runResponse(exeFile,sessID,textFile,textData));
    }

}


function verifResponse(response, theID){

    return function(error,stdout,stderr){
        var verif = false;
        for(c in stdout){
            if(c === '~'){
                verif = true;
                break;
            }
        }
        response.json({
            sessID: theID,
            verified: verif,
            failed: (error === null)
        });
    }

}


function progResponse(response, theID, theFile, session, field){
    fs.readFile(session.filePath+"/intermediates/prog.txt",
        (function(err,data){
            var prog;
            if(data !== null){
                prog = data.length;
            }
            else{
                prog = 0;
            }
            fs.readFile(theFile,
                (function(err,data){
                    if(data !== null){
                        session.state[field] = data;
                        response.json({
                            sessID: theID,
                            progress: prog,
                            data: data,
                            failed: false
                        });
                    }
                    else{
                        response.json({
                            sessID: theID,
                            progress: prog,
                            data: null,
                            failed: false
                        });
                    }
                })
            );
        })
    );
}


app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

app.post('/', (request, response) => {

    var data = request.body;

    var stage = data.stage;
    var sessData;
    var sessID;
    if(stage === 0){
        sessData = makeSession();
        sessID = sessData.id;
        sessions[sessID] = sessData;
    }
    else{
        sessID = data.sessID;
        sessData = sessions[sessID];
    }

    switch(stage){
        //================================//================================//================================
        case 0:
            setupSession(sessData.filePath,sessData.models);
            execResponse("FastenerDetection.exe",sessID,"","");
            break;
        //================================//================================//================================
        case 1:
            progResponse(response, sessID, sessData.filePath+"/XML/parts_properties.xml", sessData, "partsPropertiesIn");
            break;
        //================================//================================//================================
        case 2:
            execResponse("DisassemblyDirections.exe",sessID,sessData.filePath+"parts_properties2.xml",textData)
            break;
        //================================//================================//================================
        case 3:
            progResponse(response, sessID, sessData.filePath+"/XML/directionList.xml", sessData, "dirConfirmIn");
            break;
        //================================//================================//================================
        case 4:
            execResponse("Verification.exe",sessID,sessData.filePath+"/XML/directionList2.xml",textData)
            break;
        //================================//================================//================================
        case 5:
            progResponse(response, sessID, sessData.filePath+"/XML/verification.xml", sessData, "dirConfirmIn");
            break;
        //================================//================================//================================
        case 6:
            execResponse("AssemblyPlanning.exe",sessID,sessData.filePath+"/XML/directionList2.xml",textData)
            break;
        //================================//================================//================================
        case 7:
            progResponse(response, sessID, sessData.filePath+"/XML/solution.xml", sessData, "renderIn");
            break;
    }

});


app.get('/', (request, response) => {

    var context = {
        jsstl:content.jsstl,
        treequence:content.treequence,
        partRender:content.partRender,
        scriptBase: content.scriptBase,
        styleBase: content.styleBase
    };

    response.send(baseTemplate(context));

});


app.get('/:stage', (request, response) => {

    var stage = request.params.stage;

    var context = {};

    switch(stage){
        case 0:
            context.stageHTML = content.uploadMain;
            context.stageScript = content.scriptBase;
            context.stageStyle = content.styleBase;
            break;
        case 1:
            context.stageHTML = content.progMain;
            context.stageScript = content.progScript;
            context.stageStyle = content.progStyle;
            break;
        case 2:
            context.stageHTML = content.partPropMain;
            context.stageScript = content.partScript;
            context.stageStyle = content.partStyle;
            break;
        case 3:
            context.stageHTML = content.progMain;
            context.stageScript = content.progScript;
            context.stageStyle = content.progStyle;
            break;
        case 4:
            context.stageHTML = content.dirConMain;
            context.stageScript = content.dirScript;
            context.stageStyle = content.dirStyle;
            break;
        case 5:
            context.stageHTML = content.progMain;
            context.stageScript = content.progScript;
            context.stageStyle = content.progStyle;
            break;
        case 6:
            context.stageHTML = content.renderMain;
            context.stageScript = content.renderScript;
            context.stageStyle = content.renderStyle;
            break;
    }
    response.send(stageTemplate(context));

});

app.listen(port, (err) => {
    if (err) {
        return console.log('something bad happened', err)
    }
    console.log(`server is listening on ${port}`)
});
