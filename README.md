# mediaportal-latest-media-handler
[![MP AnyCPU](https://img.shields.io/badge/MP-AnyCPU-blue?logo=windows&logoColor=white)](https://github.com/andrewjswan/mediaportal-latest-media-handler/releases)
[![Build status](https://ci.appveyor.com/api/projects/status/9gay73f8e62pr8v6/branch/master?svg=true)](https://ci.appveyor.com/project/andrewjswan79536/mediaportal-latest-media-handler/branch/master)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/andrewjswan/mediaportal-latest-media-handler/build.yml?logo=github)](https://github.com/andrewjswan/mediaportal-latest-media-handler/actions)
[![GitHub](https://img.shields.io/github/license/andrewjswan/mediaportal-latest-media-handler?color=blue)](https://github.com/andrewjswan/mediaportal-latest-media-handler/blob/master/LICENSE)
[![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/andrewjswan/mediaportal-latest-media-handler?include_prereleases)](https://github.com/andrewjswan/mediaportal-latest-media-handler/releases)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/downloads-pre/andrewjswan/mediaportal-latest-media-handler/latest/total?label=release@downloads)](https://github.com/andrewjswan/mediaportal-latest-media-handler/releases)
[![StandWithUkraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://github.com/vshymanskyy/StandWithUkraine/blob/main/docs/README.md)

Latest Media Handler is a plugin for MediaPortal (MP).

The plugin supports pushing latest added media to your skin.

* latest added Pictures<br/>
* latest added Music<br/>
* latest added Videos<br/>
* latest added Series (TVSeries)<br/>
* latest added Movies (Moving Pictures)<br/>
* latest added Films  (My Films)<br/>
* latest added Music Videos (mvCentral)<br/>
* latest added TV Recordings<br/>

SkinnersGuide: https://code.google.com/p/latestmediahandler/wiki/SkinnersGuide

Automatically exported from code.google.com/p/mediaportal-latest-media-handler

------------------------------------------------------------------------------
## Defines
<pre>
#LatestMediaHandler:Yes
</pre>

## List of latests properties:
<pre>
* Global
 #latestMediaHandler.scanned

* Music:
 #latestMediaHandler.music.label
 #latestMediaHandler.music.latest.enabled
 #latestMediaHandler.music.hasnew
 #latestMediaHandler.music.latest.mode
 #latestMediaHandler.music.latest.thumbtype
 #latestMediaHandler.music.latest[1..N*].thumb
 #latestMediaHandler.music.latest[1..N*].artist
 #latestMediaHandler.music.latest[1..N*].artistbio
 #latestMediaHandler.music.latest[1..N*].artistbiooutline
 #latestMediaHandler.music.latest[1..N*].album
 #latestMediaHandler.music.latest[1..N*].dateAdded
 #latestMediaHandler.music.latest[1..N*].fanart
 #latestMediaHandler.music.latest[1..N*].genre
 #latestMediaHandler.music.latest[1..N*].new
 #latestMediaHandler.music.latest[1..N*].banner
 #latestMediaHandler.music.latest[1..N*].clearart
 #latestMediaHandler.music.latest[1..N*].clearlogo
 #latestMediaHandler.music.latest[1..N*].cd

 #latestMediaHandler.music.selected.thumb
 #latestMediaHandler.music.selected.artist
 #latestMediaHandler.music.selected.artistbio
 #latestMediaHandler.music.selected.artistbiooutline
 #latestMediaHandler.music.selected.album
 #latestMediaHandler.music.selected.dateAdded
 #latestMediaHandler.music.selected.genre
 #latestMediaHandler.music.selected.new
 #latestMediaHandler.music.selected.fanart1
 #latestMediaHandler.music.selected.fanart2
 #latestMediaHandler.music.selected.showfanart1
 #latestMediaHandler.music.selected.showfanart2
 #latestMediaHandler.music.selected.banner
 #latestMediaHandler.music.selected.clearart
 #latestMediaHandler.music.selected.clearlogo
 #latestMediaHandler.music.selected.cd

* MyVideo
 #latestMediaHandler.myvideo.label
 #latestMediaHandler.myvideo.latest.enabled
 #latestMediaHandler.myvideo.hasnew
 #latestMediaHandler.myvideo.latest[1..N*].thumb
 #latestMediaHandler.myvideo.latest[1..N*].fanart
 #latestMediaHandler.myvideo.latest[1..N*].title
 #latestMediaHandler.myvideo.latest[1..N*].dateAdded
 #latestMediaHandler.myvideo.latest[1..N*].genre
 #latestMediaHandler.myvideo.latest[1..N*].rating
 #latestMediaHandler.myvideo.latest[1..N*].roundedRating
 #latestMediaHandler.myvideo.latest[1..N*].classification
 #latestMediaHandler.myvideo.latest[1..N*].runtime
 #latestMediaHandler.myvideo.latest[1..N*].year
 #latestMediaHandler.myvideo.latest[1..N*].id
 #latestMediaHandler.myvideo.latest[1..N*].plot
 #latestMediaHandler.myvideo.latest[1..N*].plotoutline
 #latestMediaHandler.myvideo.latest[1..N*].new
 #latestMediaHandler.myvideo.latest[1..N*].banner
 #latestMediaHandler.myvideo.latest[1..N*].clearart
 #latestMediaHandler.myvideo.latest[1..N*].clearlogo
 #latestMediaHandler.myvideo.latest[1..N*].cd
 #latestMediaHandler.myvideo.latest[1..N*].aniposter
 #latestMediaHandler.myvideo.latest[1..N*].anibackground

 #latestMediaHandler.myvideo.selected.thumb
 #latestMediaHandler.myvideo.selected.title
 #latestMediaHandler.myvideo.selected.dateAdded
 #latestMediaHandler.myvideo.selected.genre
 #latestMediaHandler.myvideo.selected.roundedRating
 #latestMediaHandler.myvideo.selected.classification
 #latestMediaHandler.myvideo.selected.runtime
 #latestMediaHandler.myvideo.selected.year
 #latestMediaHandler.myvideo.selected.id
 #latestMediaHandler.myvideo.selected.plot
 #latestMediaHandler.myvideo.selected.plotoutline
 #latestMediaHandler.myvideo.selected.new
 #latestMediaHandler.myvideo.selected.fanart1
 #latestMediaHandler.myvideo.selected.fanart2
 #latestMediaHandler.myvideo.selected.showfanart1
 #latestMediaHandler.myvideo.selected.showfanart2
 #latestMediaHandler.myvideo.selected.banner
 #latestMediaHandler.myvideo.selected.clearart
 #latestMediaHandler.myvideo.selected.clearlogo
 #latestMediaHandler.myvideo.selected.cd
 #latestMediaHandler.myvideo.selected.aniposter
 #latestMediaHandler.myvideo.selected.anibackground

* Pictures
 #latestMediaHandler.picture.label
 #latestMediaHandler.picture.latest.enabled
 #latestMediaHandler.picture.hasnew
 #latestMediaHandler.picture.latest[1..N*].title
 #latestMediaHandler.picture.latest[1..N*].thumb
 #latestMediaHandler.picture.latest[1..N*].filename
 #latestMediaHandler.picture.latest[1..N*].fanart
 #latestMediaHandler.picture.latest[1..N*].dateAdded
 #latestMediaHandler.picture.latest[1..N*].new

 #latestMediaHandler.picture.selected.thumb
 #latestMediaHandler.picture.selected.title
 #latestMediaHandler.picture.selected.dateAdded
 #latestMediaHandler.picture.selected.filename
 #latestMediaHandler.picture.selected.new
 #latestMediaHandler.picture.selected.fanart1
 #latestMediaHandler.picture.selected.fanart2
 #latestMediaHandler.picture.selected.showfanart1
 #latestMediaHandler.picture.selected.showfanart2

* TV Series
 #latestMediaHandler.tvseries.label
 #latestMediaHandler.tvseries.latest.enabled
 #latestMediaHandler.tvseries.hasnew
 #latestMediaHandler.tvseries.latest.mode - episodes, seasons, series
 #latestMediaHandler.tvseries.latest.type
 #latestMediaHandler.tvseries.latest.thumbtype
 #latestMediaHandler.tvseries.latest[1..N*].thumb
 #latestMediaHandler.tvseries.latest[1..N*].serieThumb
 #latestMediaHandler.tvseries.latest[1..N*].fanart
 #latestMediaHandler.tvseries.latest[1..N*].serieName
 #latestMediaHandler.tvseries.latest[1..N*].seasonIndex
 #latestMediaHandler.tvseries.latest[1..N*].episodeName
 #latestMediaHandler.tvseries.latest[1..N*].episodeIndex
 #latestMediaHandler.tvseries.latest[1..N*].dateAdded
 #latestMediaHandler.tvseries.latest[1..N*].genre
 #latestMediaHandler.tvseries.latest[1..N*].rating
 #latestMediaHandler.tvseries.latest[1..N*].roundedRating
 #latestMediaHandler.tvseries.latest[1..N*].classification
 #latestMediaHandler.tvseries.latest[1..N*].runtime
 #latestMediaHandler.tvseries.latest[1..N*].firstAired
 #latestMediaHandler.tvseries.latest[1..N*].plot
 #latestMediaHandler.tvseries.latest[1..N*].plotoutline
 #latestMediaHandler.tvseries.latest[1..N*].new
 #latestMediaHandler.tvseries.latest[1..N*].banner
 #latestMediaHandler.tvseries.latest[1..N*].clearart
 #latestMediaHandler.tvseries.latest[1..N*].clearlogo
 #latestMediaHandler.tvseries.latest[1..N*].cd

 #latestMediaHandler.tvseries.selected.thumb
 #latestMediaHandler.tvseries.selected.serieThumb
 #latestMediaHandler.tvseries.selected.serieName
 #latestMediaHandler.tvseries.selected.seasonIndex
 #latestMediaHandler.tvseries.selected.episodeName
 #latestMediaHandler.tvseries.selected.episodeIndex
 #latestMediaHandler.tvseries.selected.dateAdded
 #latestMediaHandler.tvseries.selected.genre
 #latestMediaHandler.tvseries.selected.rating
 #latestMediaHandler.tvseries.selected.roundedRating
 #latestMediaHandler.tvseries.selected.classification
 #latestMediaHandler.tvseries.selected.runtime
 #latestMediaHandler.tvseries.selected.firstAired
 #latestMediaHandler.tvseries.selected.plot
 #latestMediaHandler.tvseries.selected.plotoutline
 #latestMediaHandler.tvseries.selected.new
 #latestMediaHandler.tvseries.selected.fanart1
 #latestMediaHandler.tvseries.selected.fanart2
 #latestMediaHandler.tvseries.selected.showfanart1
 #latestMediaHandler.tvseries.selected.showfanart2
 #latestMediaHandler.tvseries.selected.banner
 #latestMediaHandler.tvseries.selected.clearart
 #latestMediaHandler.tvseries.selected.clearlogo
 #latestMediaHandler.tvseries.selected.cd

* Moving Pictures:
 #latestMediaHandler.movingpicture.label
 #latestMediaHandler.movingpicture.latest.enabled
 #latestMediaHandler.movingpicture.hasnew
 #latestMediaHandler.movingpicture.latest[1..N*].thumb
 #latestMediaHandler.movingpicture.latest[1..N*].fanart
 #latestMediaHandler.movingpicture.latest[1..N*].title
 #latestMediaHandler.movingpicture.latest[1..N*].dateAdded
 #latestMediaHandler.movingpicture.latest[1..N*].genre
 #latestMediaHandler.movingpicture.latest[1..N*].rating
 #latestMediaHandler.movingpicture.latest[1..N*].roundedRating
 #latestMediaHandler.movingpicture.latest[1..N*].classification
 #latestMediaHandler.movingpicture.latest[1..N*].runtime
 #latestMediaHandler.movingpicture.latest[1..N*].year
 #latestMediaHandler.movingpicture.latest[1..N*].id
 #latestMediaHandler.movingpicture.latest[1..N*].plot
 #latestMediaHandler.movingpicture.latest[1..N*].plotoutline
 #latestMediaHandler.movingpicture.latest[1..N*].new
 #latestMediaHandler.movingpicture.latest[1..N*].banner
 #latestMediaHandler.movingpicture.latest[1..N*].clearart
 #latestMediaHandler.movingpicture.latest[1..N*].clearlogo
 #latestMediaHandler.movingpicture.latest[1..N*].cd
 #latestMediaHandler.movingpicture.latest[1..N*].aniposter
 #latestMediaHandler.movingpicture.latest[1..N*].anibackground
  
 #latestMediaHandler.movingpicture.selected.thumb
 #latestMediaHandler.movingpicture.selected.title
 #latestMediaHandler.movingpicture.selected.dateAdded
 #latestMediaHandler.movingpicture.selected.genre
 #latestMediaHandler.movingpicture.selected.rating
 #latestMediaHandler.movingpicture.selected.roundedRating
 #latestMediaHandler.movingpicture.selected.classification
 #latestMediaHandler.movingpicture.selected.runtime
 #latestMediaHandler.movingpicture.selected.year
 #latestMediaHandler.movingpicture.selected.id
 #latestMediaHandler.movingpicture.selected.plot
 #latestMediaHandler.movingpicture.selected.plotoutline
 #latestMediaHandler.movingpicture.selected.new
 #latestMediaHandler.movingpicture.selected.fanart1
 #latestMediaHandler.movingpicture.selected.fanart2
 #latestMediaHandler.movingpicture.selected.showfanart1
 #latestMediaHandler.movingpicture.selected.showfanart2
 #latestMediaHandler.movingpicture.selected.banner
 #latestMediaHandler.movingpicture.selected.clearart
 #latestMediaHandler.movingpicture.selected.clearlogo
 #latestMediaHandler.movingpicture.selected.cd
 #latestMediaHandler.movingpicture.selected.aniposter
 #latestMediaHandler.movingpicture.selected.anibackground

* MyFilms
 #latestMediaHandler.myfilms.label
 #latestMediaHandler.myfilms.latest.enabled
 #latestMediaHandler.myfilms.hasnew
 #latestMediaHandler.myfilms.latest[1..N*].thumb
 #latestMediaHandler.myfilms.latest[1..N*].fanart
 #latestMediaHandler.myfilms.latest[1..N*].title
 #latestMediaHandler.myfilms.latest[1..N*].dateAdded
 #latestMediaHandler.myfilms.latest[1..N*].rating
 #latestMediaHandler.myfilms.latest[1..N*].roundedRating
 #latestMediaHandler.myfilms.latest[1..N*].year
 #latestMediaHandler.myfilms.latest[1..N*].id
 #latestMediaHandler.myfilms.latest[1..N*].plot
 #latestMediaHandler.myfilms.latest[1..N*].plotoutline
 #latestMediaHandler.myfilms.latest[1..N*].new
 #latestMediaHandler.myfilms.latest[1..N*].banner
 #latestMediaHandler.myfilms.latest[1..N*].clearart
 #latestMediaHandler.myfilms.latest[1..N*].clearlogo
 #latestMediaHandler.myfilms.latest[1..N*].cd
 #latestMediaHandler.myfilms.latest[1..N*].aniposter
 #latestMediaHandler.myfilms.latest[1..N*].anibackground

 #latestMediaHandler.myfilms.selected.thumb
 #latestMediaHandler.myfilms.selected.title
 #latestMediaHandler.myfilms.selected.dateAdded
 #latestMediaHandler.myfilms.selected.genre
 #latestMediaHandler.myfilms.selected.roundedRating
 #latestMediaHandler.myfilms.selected.classification
 #latestMediaHandler.myfilms.selected.runtime
 #latestMediaHandler.myfilms.selected.year
 #latestMediaHandler.myfilms.selected.id
 #latestMediaHandler.myfilms.selected.plot
 #latestMediaHandler.myfilms.selected.plotoutline
 #latestMediaHandler.myfilms.selected.new
 #latestMediaHandler.myfilms.selected.fanart1
 #latestMediaHandler.myfilms.selected.fanart2
 #latestMediaHandler.myfilms.selected.showfanart1
 #latestMediaHandler.myfilms.selected.showfanart2
 #latestMediaHandler.myfilms.selected.banner
 #latestMediaHandler.myfilms.selected.clearart
 #latestMediaHandler.myfilms.selected.clearlogo
 #latestMediaHandler.myfilms.selected.cd
 #latestMediaHandler.myfilms.selected.aniposter
 #latestMediaHandler.myfilms.selected.anibackground

* MvCentral:
 #latestMediaHandler.mvcentral.label
 #latestMediaHandler.mvcentral.latest.enabled
 #latestMediaHandler.mvcentral.hasnew
 #latestMediaHandler.mvcentral.latest.mode
 #latestMediaHandler.mvcentral.latest.thumbtype
 #latestMediaHandler.mvcentral.latest[1..N*].thumb
 #latestMediaHandler.mvcentral.latest[1..N*].artist
 #latestMediaHandler.mvcentral.latest[1..N*].artistbio
 #latestMediaHandler.mvcentral.latest[1..N*].artistbiooutline
 #latestMediaHandler.mvcentral.latest[1..N*].album
 #latestMediaHandler.mvcentral.latest[1..N*].track
 #latestMediaHandler.mvcentral.latest[1..N*].dateAdded
 #latestMediaHandler.mvcentral.latest[1..N*].fanart
 #latestMediaHandler.mvcentral.latest[1..N*].genre
 #latestMediaHandler.mvcentral.latest[1..N*].new
 #latestMediaHandler.mvcentral.latest[1..N*].banner
 #latestMediaHandler.mvcentral.latest[1..N*].clearart
 #latestMediaHandler.mvcentral.latest[1..N*].clearlogo
 #latestMediaHandler.mvcentral.latest[1..N*].cd

 #latestMediaHandler.mvcentral.selected.thumb
 #latestMediaHandler.mvcentral.selected.artist
 #latestMediaHandler.mvcentral.selected.artistbio
 #latestMediaHandler.mvcentral.selected.artistbiooutline
 #latestMediaHandler.mvcentral.selected.album
 #latestMediaHandler.mvcentral.selected.track
 #latestMediaHandler.mvcentral.selected.dateAdded
 #latestMediaHandler.mvcentral.selected.genre
 #latestMediaHandler.mvcentral.selected.new
 #latestMediaHandler.mvcentral.selected.fanart1
 #latestMediaHandler.mvcentral.selected.fanart2
 #latestMediaHandler.mvcentral.selected.showfanart1
 #latestMediaHandler.mvcentral.selected.showfanart2
 #latestMediaHandler.mvcentral.selected.banner
 #latestMediaHandler.mvcentral.selected.clearart
 #latestMediaHandler.mvcentral.selected.clearlogo
 #latestMediaHandler.mvcentral.selected.cd

* TVRecording
 #latestMediaHandler.tvrecordings.label
 #latestMediaHandler.tvrecordings.latest.enabled
 #latestMediaHandler.tvrecordings.hasnew
 #latestMediaHandler.tvrecordings.reddot

 #latestMediaHandler.tvrecordings.active.count
 #latestMediaHandler.tvrecordings.active[1..N*].title
 #latestMediaHandler.tvrecordings.active[1..N*].genre
 #latestMediaHandler.tvrecordings.active[1..N*].startTime
 #latestMediaHandler.tvrecordings.active[1..N*].startDate
 #latestMediaHandler.tvrecordings.active[1..N*].endTime
 #latestMediaHandler.tvrecordings.active[1..N*].endDate
 #latestMediaHandler.tvrecordings.active[1..N*].channel
 #latestMediaHandler.tvrecordings.active[1..N*].channelLogo
 #latestMediaHandler.tvrecordings.active[1..N*].directory

 #latestMediaHandler.tvrecordings.scheduled.count
 #latestMediaHandler.tvrecordings.scheduled[1..N*].title
 #latestMediaHandler.tvrecordings.scheduled[1..N*].startTime
 #latestMediaHandler.tvrecordings.scheduled[1..N*].startDate
 #latestMediaHandler.tvrecordings.scheduled[1..N*].endTime
 #latestMediaHandler.tvrecordings.scheduled[1..N*].endDate
 #latestMediaHandler.tvrecordings.scheduled[1..N*].channel
 #latestMediaHandler.tvrecordings.scheduled[1..N*].channelLogo

 #latestMediaHandler.tvrecordings.latest[1..N*].thumb
 #latestMediaHandler.tvrecordings.latest[1..N*].title
 #latestMediaHandler.tvrecordings.latest[1..N*].dateAdded
 #latestMediaHandler.tvrecordings.latest[1..N*].genre
 #latestMediaHandler.tvrecordings.latest[1..N*].new
 #latestMediaHandler.tvrecordings.latest[1..N*].summary
 #latestMediaHandler.tvrecordings.latest[1..N*].summaryoutline
 #latestMediaHandler.tvrecordings.latest[1..N*].series
 #latestMediaHandler.tvrecordings.latest[1..N*].episode
 #latestMediaHandler.tvrecordings.latest[1..N*].episodename
 #latestMediaHandler.tvrecordings.latest[1..N*].directory
  
 #latestMediaHandler.tvrecordings.selected.thumb
 #latestMediaHandler.tvrecordings.selected.title
 #latestMediaHandler.tvrecordings.selected.dateAdded
 #latestMediaHandler.tvrecordings.selected.genre
 #latestMediaHandler.tvrecordings.selected.startTime
 #latestMediaHandler.tvrecordings.selected.endTime
 #latestMediaHandler.tvrecordings.selected.summary
 #latestMediaHandler.tvrecordings.selected.summaryoutline
 #latestMediaHandler.tvrecordings.selected.directory
 #latestMediaHandler.tvrecordings.selected.new
 #latestMediaHandler.tvrecordings.selected.fanart1
 #latestMediaHandler.tvrecordings.selected.fanart2
 #latestMediaHandler.tvrecordings.selected.showfanart1
 #latestMediaHandler.tvrecordings.selected.showfanart2

 * N - Depend from LatestMediaHandler skin settings - default 4, max 10
</pre>

Facade & Buttons IDs: https://github.com/yoavain/mediaportal-latest-media-handler/blob/master/IDs.md
