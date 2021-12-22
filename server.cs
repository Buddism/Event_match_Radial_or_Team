registerOutputEvent(fxDtsBrick, "matchSlayerTeam", "string 128 200" TAB "list Players 0 Bots 1 Both 2");
registerInputEvent(fxDtsBrick, "onSlayerTeamMatchPlayer", "Self fxDtsBrick\tPlayer Player\tClient GameConnection\tMinigame Minigame");
registerInputEvent(fxDtsBrick, "onSlayerTeamMatchBot", "Self fxDtsBrick\tPlayer Player\tClient GameConnection\tMinigame Minigame");

function fxDtsBrick::matchSlayerTeam(%this, %match, %type)
{
	if(!isFunction("GameConnection", "getTeam"))
		return; //you don't have slayer enabled?
	
	if(%type == 0 || %type == 2)
	{
		for(%i = clientGroup.getCount()-1; %i >= 0; %i--)
		{
			%client = clientGroup.getObject(%i);
			if(%client.getTeam().title $= %match)
			{
				$InputTarget_["Self"] 		= %this;
				$InputTarget_["Player"] 	= isObject(%client.player) ? %client.player : 0;
				$InputTarget_["Bot"] 		= 0;
				$InputTarget_["Client"] 	= %client;
				$InputTarget_["MiniGame"] 	= getMiniGameFromObject (%client);

				%this.processInputEvent("onSlayerTeamMatchPlayer", %client);
			}
		}
	}
	if(%type == 1 || %type == 2)
	{
		for(%i = mainHoleBotSet.getCount() - 1; %i >= 0; %i--)
		{
			%aiplayer = mainHoleBotSet.getObject(%i);
			if(%aiplayer.getTeam().title $= %match)
			{
				$InputTarget_["Self"] 		= %this;
				$InputTarget_["Player"] 	= %aiplayer;
				$InputTarget_["Bot"] 		= %aiplayer;
				$InputTarget_["aiplayer"] 	= %aiplayer;
				$InputTarget_["MiniGame"] 	= getMiniGameFromObject (%aiplayer);

				%this.processInputEvent("onSlayerTeamMatchBot", %aiplayer);
			}
		}
	}
}

registerOutputEvent(fxDtsBrick, "matchRadialCheck", "list Player 0 Bot 1 Brick 2 Vehicle 4 Projectile 5" TAB "string 50 100" TAB "string 50 100" TAB "string 50 100");
registerInputEvent(fxDtsBrick, "onRadialMatch", "Self fxDtsBrick" TAB "Bot Bot" TAB "Player Player" TAB "Client GameConnection" TAB "Vehicle Vehicle" TAB "Projectile Projectile" TAB "Minigame Minigame");

function fxDtsBrick::matchRadialCheck(%this, %typeID, %radius, %bounds, %team)
{
	if(!isFunction("GameConnection", "getTeam"))
		%doTeamCheck = false;
	else
		%doTeamCheck = %team !$= "";

	%types[0] = $TypeMasks::PlayerObjectType; //Player
	%types[1] = $TypeMasks::PlayerObjectType; //Bot
	%types[2] = $TypeMasks::FxBrickAlwaysObjectType; //Brick
	%types[4] = $TypeMasks::VehicleObjectType;
	%types[5] = $TypeMasks::ProjectileObjectType;

	%typeMask = %types[%typeID];
	%radius = %radius * 1;

	%boundsLow = getWord(%bounds, 0);
	%boundsHigh = getWord(%bounds, 1);
	if(%boundsHigh !$= "" && %boundsLow !$= "")
	{
		for(%i = 0; %i < %this.numEvents; %i++)
		{
			if(%i < %boundsLow || %i > %boundsHigh) //non-matching
			{
				if(%this.eventInput[%i] $= "onRadialMatch")
				{
					%oldEnabled[%i] = %this.eventEnabled[%i];
					%this.eventEnabled[%i] = 0;
				}
			}
		}
	}

	%position = %this.getPosition();
	initContainerRadiusSearch(%position, %radius, %typeMask);
	while(%object = containerSearchNext())
	{
		if(%object == %this)
			continue;

		if(%typeID == 2)
		{
			$InputTarget_["Self"] 		= %object; //weird to think about
			$InputTarget_["Bot"] 		= %object.hBot;
			$InputTarget_["Vehicle"] 	= %object.vehicle;
			if(!$Server::Lan && isObject(%client = %object.getGroup().getClient()))
			{
				$InputTarget_["Client"] = %client;
				$InputTarget_["Player"] = %client.player;
			}
			$InputTarget_["Projectile"] = 0;
			$InputTarget_["MiniGame"] 	= getMiniGameFromObject (%object);

		} else {
			$InputTarget_["Self"] = %this;

			switch(%typeID)
			{
				case 0: // PLAYER
					if(%object.getClassName() !$= "Player")
						continue; //skip AIPLAYERS, PLAYERS only
					if(%doTeamCheck && isObject(%object.client) && %object.client.getTeam().title !$= %team)
						continue;

					$InputTarget_["Player"] 	= %object;
					$InputTarget_["Client"] 	= %object.client;
					$InputTarget_["Bot"] 		= %object; //this could end poorly but here we go
					$InputTarget_["Projectile"] = 0;
					$InputTarget_["Vehicle"] 	= 0;
				case 1: // BOT
					if(%object.getClassName() !$= "AiPlayer")
						continue; //skip AIPLAYERS, PLAYERS only
					if(%doTeamCheck && %object.getTeam().title !$= %team)
						continue;
						
					$InputTarget_["Player"] 	= %object;
					$InputTarget_["Client"] 	= %object; //hbot clients are themselves i believe
					$InputTarget_["Bot"] 		= %object;
					$InputTarget_["Projectile"] = 0;
					$InputTarget_["Vehicle"] 	= 0;
				case 4: // VEHICLE
					$InputTarget_["Player"] 	= %object.getControllingObject();
					$InputTarget_["Client"] 	= %object.getControllingClient();
					$InputTarget_["Bot"] 		= %object.getControllingObject();
					$InputTarget_["Projectile"] = 0;
					$InputTarget_["Vehicle"] 	= %object;
				case 5: // PROJECTILE
					$InputTarget_["Projectile"] = %object;
					$InputTarget_["Player"] 	= %object.sourceObject;
					$InputTarget_["Client"] 	= %object.client;
					$InputTarget_["Vehicle"] 	= 0;
					$InputTarget_["Bot"] 		= 0;
			}
			$InputTarget_["MiniGame"] 			= getMiniGameFromObject (%object);
		}
		
		%this.processInputEvent("onRadialMatch", $InputTarget_["Client"]); //might anger quotaobjects
	}

	if(%boundsHigh !$= "" && %boundsLow !$= "")
	{
		for(%i = 0; %i < %this.numEvents; %i++)
		{
			if(%i < %boundsLow || %i > %boundsHigh) //non-matching
			{
				if(%this.eventInput[%i] $= "onRadialMatch")
					%this.eventEnabled[%i] = %oldEnabled[%i];
			}
		}
	}
}