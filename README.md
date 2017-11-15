# mediaportal-latest-media-handler
Latest Media Handler is a plugin for MediaPortal (MP).

The plugin supports pushing latest added media to your skin.

* latest added Pictures<br/>
* latest added Music<br/>
* latest added Videos<br/>
* latest added Series (TVSeries)<br/>
* latest added Movies (Moving Pictures)<br/>
* latest added Films  (My Films)<br/>
* latest added Videos (mvCentral)<br/>
* latest added TV Recordings<br/>

SkinnersGuide: https://code.google.com/p/latestmediahandler/wiki/SkinnersGuide

Automatically exported from code.google.com/p/mediaportal-latest-media-handler

------------------------------------------------------------------------------
List of latests properties:
<pre>
* Global
 #latestMediaHandler.scanned

* Music:
 #latestMediaHandler.music.label
 #latestMediaHandler.music.latest.enabled
 #latestMediaHandler.music.hasnew
 #latestMediaHandler.music.latest[1,2,3,4].thumb
 #latestMediaHandler.music.latest[1,2,3,4].artist
 #latestMediaHandler.music.latest[1,2,3,4].artistbio
 #latestMediaHandler.music.latest[1,2,3,4].artistbiooutline
 #latestMediaHandler.music.latest[1,2,3,4].album
 #latestMediaHandler.music.latest[1,2,3,4].dateAdded
 #latestMediaHandler.music.latest[1,2,3,4].fanart
 #latestMediaHandler.music.latest[1,2,3,4].genre
 #latestMediaHandler.music.latest[1,2,3,4].new
 #latestMediaHandler.music.latest[1,2,3,4].banner
 #latestMediaHandler.music.latest[1,2,3,4].clearart
 #latestMediaHandler.music.latest[1,2,3,4].clearlogo
 #latestMediaHandler.music.latest[1,2,3,4].cd

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
 #latestMediaHandler.myvideo.latest[1,2,3,4].thumb
 #latestMediaHandler.myvideo.latest[1,2,3,4].fanart
 #latestMediaHandler.myvideo.latest[1,2,3,4].title
 #latestMediaHandler.myvideo.latest[1,2,3,4].dateAdded
 #latestMediaHandler.myvideo.latest[1,2,3,4].genre
 #latestMediaHandler.myvideo.latest[1,2,3,4].rating
 #latestMediaHandler.myvideo.latest[1,2,3,4].roundedRating
 #latestMediaHandler.myvideo.latest[1,2,3,4].classification
 #latestMediaHandler.myvideo.latest[1,2,3,4].runtime
 #latestMediaHandler.myvideo.latest[1,2,3,4].year
 #latestMediaHandler.myvideo.latest[1,2,3,4].id
 #latestMediaHandler.myvideo.latest[1,2,3,4].plot
 #latestMediaHandler.myvideo.latest[1,2,3,4].plotoutline
 #latestMediaHandler.myvideo.latest[1,2,3,4].new
 #latestMediaHandler.myvideo.latest[1,2,3,4].banner
 #latestMediaHandler.myvideo.latest[1,2,3,4].clearart
 #latestMediaHandler.myvideo.latest[1,2,3,4].clearlogo
 #latestMediaHandler.myvideo.latest[1,2,3,4].cd

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

* Pictures
 #latestMediaHandler.picture.label
 #latestMediaHandler.picture.latest.enabled
 #latestMediaHandler.picture.hasnew
 #latestMediaHandler.picture.latest[1,2,3,4].title
 #latestMediaHandler.picture.latest[1,2,3,4].thumb
 #latestMediaHandler.picture.latest[1,2,3,4].filename
 #latestMediaHandler.picture.latest[1,2,3,4].fanart
 #latestMediaHandler.picture.latest[1,2,3,4].dateAdded
 #latestMediaHandler.picture.latest[1,2,3,4].new

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
 #latestMediaHandler.tvseries.latest[1,2,3,4].thumb
 #latestMediaHandler.tvseries.latest[1,2,3,4].serieThumb
 #latestMediaHandler.tvseries.latest[1,2,3,4].fanart
 #latestMediaHandler.tvseries.latest[1,2,3,4].serieName
 #latestMediaHandler.tvseries.latest[1,2,3,4].seasonIndex
 #latestMediaHandler.tvseries.latest[1,2,3,4].episodeName
 #latestMediaHandler.tvseries.latest[1,2,3,4].episodeIndex
 #latestMediaHandler.tvseries.latest[1,2,3,4].dateAdded
 #latestMediaHandler.tvseries.latest[1,2,3,4].genre
 #latestMediaHandler.tvseries.latest[1,2,3,4].rating
 #latestMediaHandler.tvseries.latest[1,2,3,4].roundedRating
 #latestMediaHandler.tvseries.latest[1,2,3,4].classification
 #latestMediaHandler.tvseries.latest[1,2,3,4].runtime
 #latestMediaHandler.tvseries.latest[1,2,3,4].firstAired
 #latestMediaHandler.tvseries.latest[1,2,3,4].plot
 #latestMediaHandler.tvseries.latest[1,2,3,4].plotoutline
 #latestMediaHandler.tvseries.latest[1,2,3,4].new
 #latestMediaHandler.tvseries.latest[1,2,3,4].banner
 #latestMediaHandler.tvseries.latest[1,2,3,4].clearart
 #latestMediaHandler.tvseries.latest[1,2,3,4].clearlogo
 #latestMediaHandler.tvseries.latest[1,2,3,4].cd

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
 #latestMediaHandler.movingpicture.latest[1,2,3,4].thumb
 #latestMediaHandler.movingpicture.latest[1,2,3,4].fanart
 #latestMediaHandler.movingpicture.latest[1,2,3,4].title
 #latestMediaHandler.movingpicture.latest[1,2,3,4].dateAdded
 #latestMediaHandler.movingpicture.latest[1,2,3,4].genre
 #latestMediaHandler.movingpicture.latest[1,2,3,4].rating
 #latestMediaHandler.movingpicture.latest[1,2,3,4].roundedRating
 #latestMediaHandler.movingpicture.latest[1,2,3,4].classification
 #latestMediaHandler.movingpicture.latest[1,2,3,4].runtime
 #latestMediaHandler.movingpicture.latest[1,2,3,4].year
 #latestMediaHandler.movingpicture.latest[1,2,3,4].id
 #latestMediaHandler.movingpicture.latest[1,2,3,4].plot
 #latestMediaHandler.movingpicture.latest[1,2,3,4].plotoutline
 #latestMediaHandler.movingpicture.latest[1,2,3,4].new
 #latestMediaHandler.movingpicture.latest[1,2,3,4].banner
 #latestMediaHandler.movingpicture.latest[1,2,3,4].clearart
 #latestMediaHandler.movingpicture.latest[1,2,3,4].clearlogo
 #latestMediaHandler.movingpicture.latest[1,2,3,4].cd
  
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

* MyFilms
 #latestMediaHandler.myfilms.label
 #latestMediaHandler.myfilms.latest.enabled
 #latestMediaHandler.myfilms.hasnew
 #latestMediaHandler.myfilms.latest[1,2,3,4].poster
 #latestMediaHandler.myfilms.latest[1,2,3,4].fanart
 #latestMediaHandler.myfilms.latest[1,2,3,4].title
 #latestMediaHandler.myfilms.latest[1,2,3,4].dateAdded
 #latestMediaHandler.myfilms.latest[1,2,3,4].rating
 #latestMediaHandler.myfilms.latest[1,2,3,4].roundedRating
 #latestMediaHandler.myfilms.latest[1,2,3,4].year
 #latestMediaHandler.myfilms.latest[1,2,3,4].id
 #latestMediaHandler.myfilms.latest[1,2,3,4].plot
 #latestMediaHandler.myfilms.latest[1,2,3,4].plotoutline
 #latestMediaHandler.myfilms.latest[1,2,3,4].new
 #latestMediaHandler.myfilms.latest[1,2,3,4].banner
 #latestMediaHandler.myfilms.latest[1,2,3,4].clearart
 #latestMediaHandler.myfilms.latest[1,2,3,4].clearlogo
 #latestMediaHandler.myfilms.latest[1,2,3,4].cd

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

* MvCentral:
 #latestMediaHandler.mvcentral.label
 #latestMediaHandler.mvcentral.latest.enabled
 #latestMediaHandler.mvcentral.hasnew
 #latestMediaHandler.mvcentral.latest[1,2,3,4].thumb
 #latestMediaHandler.mvcentral.latest[1,2,3,4].artist
 #latestMediaHandler.mvcentral.latest[1,2,3,4].artistbio
 #latestMediaHandler.mvcentral.latest[1,2,3,4].artistbiooutline
 #latestMediaHandler.mvcentral.latest[1,2,3,4].album
 #latestMediaHandler.mvcentral.latest[1,2,3,4].track
 #latestMediaHandler.mvcentral.latest[1,2,3,4].dateAdded
 #latestMediaHandler.mvcentral.latest[1,2,3,4].fanart
 #latestMediaHandler.mvcentral.latest[1,2,3,4].genre
 #latestMediaHandler.mvcentral.latest[1,2,3,4].new
 #latestMediaHandler.mvcentral.latest[1,2,3,4].banner
 #latestMediaHandler.mvcentral.latest[1,2,3,4].clearart
 #latestMediaHandler.mvcentral.latest[1,2,3,4].clearlogo
 #latestMediaHandler.mvcentral.latest[1,2,3,4].cd

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

 #latestMediaHandler.tvrecordings.active[1,2,3,4].title
 #latestMediaHandler.tvrecordings.active[1,2,3,4].genre
 #latestMediaHandler.tvrecordings.active[1,2,3,4].startTime
 #latestMediaHandler.tvrecordings.active[1,2,3,4].startDate
 #latestMediaHandler.tvrecordings.active[1,2,3,4].endTime
 #latestMediaHandler.tvrecordings.active[1,2,3,4].endDate
 #latestMediaHandler.tvrecordings.active[1,2,3,4].channel
 #latestMediaHandler.tvrecordings.active[1,2,3,4].channelLogo

 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].title
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].startTime
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].startDate
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].endTime
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].endDate
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].channel
 #latestMediaHandler.tvrecordings.scheduled[1,2,3,4].channelLogo

 #latestMediaHandler.tvrecordings.latest[1,2,3,4].thumb
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].title
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].dateAdded
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].genre
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].new
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].summary
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].summaryoutline
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].series
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].episode
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].episodename
  
 #latestMediaHandler.tvrecordings.selected.thumb
 #latestMediaHandler.tvrecordings.selected.title
 #latestMediaHandler.tvrecordings.selected.dateAdded
 #latestMediaHandler.tvrecordings.selected.genre
 #latestMediaHandler.tvrecordings.selected.startTime
 #latestMediaHandler.tvrecordings.selected.endTime
 #latestMediaHandler.tvrecordings.selected.summary
 #latestMediaHandler.tvrecordings.selected.summaryoutline
 #latestMediaHandler.tvrecordings.selected.new
 #latestMediaHandler.tvrecordings.selected.fanart1
 #latestMediaHandler.tvrecordings.selected.fanart2
 #latestMediaHandler.tvrecordings.selected.showfanart1
 #latestMediaHandler.tvrecordings.selected.showfanart2
</pre>

Facade & Buttons IDs: https://github.com/yoavain/mediaportal-latest-media-handler/blob/master/IDs.md
