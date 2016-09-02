ALTER TABLE `guilds` CHANGE `introMessage` `introMessage` VARCHAR(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '', CHANGE `welcomeMessage` `welcomeMessage` VARCHAR(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '', CHANGE `leavingMessage` `leavingMessage` VARCHAR(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '', CHANGE `rejectionMessage` `rejectionMessage` VARCHAR(1000) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT '', CHANGE `type` `type` INT(11) NOT NULL DEFAULT '1', CHANGE `level` `level` INT(11) NOT NULL DEFAULT '0', CHANGE `options` `options` INT(11) NOT NULL DEFAULT '0', CHANGE `stonePropId` `stonePropId` INT(11) NOT NULL DEFAULT '0', CHANGE `stoneRegionId` `stoneRegionId` INT(11) NOT NULL DEFAULT '0', CHANGE `stoneX` `stoneX` INT(11) NOT NULL DEFAULT '0', CHANGE `stoneY` `stoneY` INT(11) NOT NULL DEFAULT '0', CHANGE `stoneDirection` `stoneDirection` FLOAT NOT NULL DEFAULT '0', CHANGE `points` `points` INT(11) NOT NULL DEFAULT '0', CHANGE `gold` `gold` INT(11) NOT NULL DEFAULT '0';