SNAKE CLIENT

Authors: Joel Ronca, Jack Doughty, & Travis Martin/Daniel Kopta
November 2023



This project contains a client that can effectively receive information about the state of a game world 
from a server based on the popular game snake. Based on the updates received, the client can effectively 
draw the current state of the world per the server’s messages. The client also has the ability to send 
control request’s to the server, in order to move its snake in basic directions(up, down, left, and 
right). The client will follow any world rules or physics outlined by the server, such as collecting 
power ups and growing its snake and what determines the snakes in the game world  to be dead or alive. 
The client will keep its snake at the center of its view, while also updating the scores and outcomes of 
other snake clients that can connect to the same server in its view as well.

Important notes for grading:

Death Animation:
	Our client will provide an animation upon the death of snakes in the game world. The snake will turn 
	red for a few moments before the snake subsequently respawns back into the game. The game will pause 
	for a few moments for the client to clearly display the death of the snake, and then the game will 
	resume.


Server Disconnect:
	We assumed we needed to display an error message alert if the server ever disconnected before the 
	client itself did. However, after observing the client provided to us, we realized that the program 
	will simply crash and close should the server ever disconnect before the client does. Therefore, 
	our program does the same and per the assignment specifications, does not need to do anymore than this. 
	We just wanted to make a note of this in this doc for grading purposes that our client follows the guidelines 
	in this aspect and behaves just as the client does.

Windows Issues:
	At one point during the PS8, we ran into a very large issue where when running the program on windows null 
	exceptions would be thrown, but the program would run perfectly fine on Mac. At one point, this led to us 
	having to just finish the assignment by working only on the Mac, as none of the TA’s were able to help us fix 
	the issue. After about 5 days, we were able to finally fix the issue and after extensive testing, the client 
	runs perfectly on the windows laptop. However, we wanted to make a note of this to the TA’s when grading this 
	assignment since the issue was so large and caused many(many) problems, we think it is something everyone should 
	be aware of, even though the problem has been fixed.

Design decision and solved problems log:

11/16/23 Design: Overall design of Client
We have decided on the overall design of our client utilizing the MVC model and it will be as follows:
- We will have a Controller, called GameController, which will reference our NetworkController, and handle connecting to the server, 
getting information from the server, and updating the games models and world.

- The Models will be the objects in the game world, powerups, snakes, and walls, which will all be kept inside a world object. This 
world object will keep a list of these game objects that are present within each frame of the game

- Our View will subscribe to delegates provided in the game model that will inform the view when a Connection has been made, when data has 
arrived from the server, and if an error has occurred during any part of the connection process.

Each time our Game controller updates the world, the View will be notified per the “DataArrived” delegate, and it will subsequently 
call the WorldPanel class, so that every updated object in the world can be redrawn for each frame.

11/17/23 Feature: Handshake between Server and Client:
	We had a few problems early on following the handshake protocol in order to connect the server and the client, so we decided to 
	utilize the design of the chat system shown in class to start our handshake and initially get the data. After the data is received, 
	we will then call a method to parse the data into json objects and update the world object.

11/20/23 Feature: Parsing as JSON objects
	We have a separate method in our game controller that parses the data sent from the server into JSON objects. We had some difficulty 
	parsing the JSON due to our JSON Constructors not working correctly. This was due to our constructors not having each necessary element of 
	the JSON objects for proper deserialization. Our program is not receiving information from the server and printing the correct data to the 
	console.

11/21/23 Problem: Bug on Windows
	We have run into a problem where mac is running and drawing the program but windows is throwing a null exception when it games to the world object 
	in the WorldPanel class. We have tried to fix it but have been unsuccessful and the TA’s have not been able to solve the problem, so we are continuing 
	the project just on Mac for now.

11/21/23 Feature: Walls Drawn
	The program now gets information from the server through the game controller and is successfully drawing the walls through the WorldPanel class in 
	their correct place and orientation.

11/22/23 Feature: Powerups and Snake Drawn
	The program now draws the powerups in the world and draws the client's snake and centers the world around that Snake through the WorldPanel class.

11/25/23 Bug Fix: Windows not running
	We were able to fix the bug where the windows laptop would not run the program by first creating a default instance of the world object in the 
	game controller before any information has been received, as well as adding some null checks before the drawing loops in the WorldPanel class.

11/25/23 Bug Fix: Walls not drawing
	A weird bug in our JSON deserialize method was causing walls to not be drawn only when the first client connects. We were able to fix this by adding 
	to ways for walls to be parsed into JSON objects. If they are sent with the first message about client and world info, they will be parsed there. 
	If they are not, then they will be parsed with the snake and powerup objects.

11/25/23 Bug Fix: Other Clients not drawing
	We had another bug where other clients would be in the game world, and its client window would be centered on it, but it would not be drawn. 
	This was due to a minor error in our drawing loop for each snake body, and now all snake clients are correctly drawn in the game world.
	
