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
 #latestMediaHandler.music.latest[1,2,3].thumb
 #latestMediaHandler.music.latest[1,2,3].artist
 #latestMediaHandler.music.latest[1,2,3].album
 #latestMediaHandler.music.latest[1,2,3].dateAdded
 #latestMediaHandler.music.latest[1,2,3].fanart
 #latestMediaHandler.music.latest[1,2,3].genre
 #latestMediaHandler.music.latest[1,2,3].new

* MyVideo
 #latestMediaHandler.myvideo.label
 #latestMediaHandler.myvideo.latest.enabled
 #latestMediaHandler.myvideo.hasnew
 #latestMediaHandler.myvideo.latest[1,2,3].thumb
 #latestMediaHandler.myvideo.latest[1,2,3].fanart
 #latestMediaHandler.myvideo.latest[1,2,3].title
 #latestMediaHandler.myvideo.latest[1,2,3].dateAdded
 #latestMediaHandler.myvideo.latest[1,2,3].genre
 #latestMediaHandler.myvideo.latest[1,2,3].rating
 #latestMediaHandler.myvideo.latest[1,2,3].roundedRating
 #latestMediaHandler.myvideo.latest[1,2,3].classification
 #latestMediaHandler.myvideo.latest[1,2,3].runtime
 #latestMediaHandler.myvideo.latest[1,2,3].year
 #latestMediaHandler.myvideo.latest[1,2,3].id
 #latestMediaHandler.myvideo.latest[1,2,3].plot
 #latestMediaHandler.myvideo.latest[1,2,3].new

* Pictures
 #latestMediaHandler.picture.label
 #latestMediaHandler.picture.latest.enabled
 #latestMediaHandler.picture.hasnew
 #latestMediaHandler.picture.latest[1,2,3].title
 #latestMediaHandler.picture.latest[1,2,3].thumb
 #latestMediaHandler.picture.latest[1,2,3].filename
 #latestMediaHandler.picture.latest[1,2,3].dateAdded
 #latestMediaHandler.picture.latest[1,2,3].new

* Moving Pictures:
 #latestMediaHandler.movingpicture.label
 #latestMediaHandler.movingpicture.latest.enabled
 #latestMediaHandler.movingpicture.hasnew
 #latestMediaHandler.movingpicture.latest[1,2,3].thumb
 #latestMediaHandler.movingpicture.latest[1,2,3].fanart
 #latestMediaHandler.movingpicture.latest[1,2,3].title
 #latestMediaHandler.movingpicture.latest[1,2,3].dateAdded
 #latestMediaHandler.movingpicture.latest[1,2,3].genre
 #latestMediaHandler.movingpicture.latest[1,2,3].rating
 #latestMediaHandler.movingpicture.latest[1,2,3].roundedRating
 #latestMediaHandler.movingpicture.latest[1,2,3].classification
 #latestMediaHandler.movingpicture.latest[1,2,3].runtime
 #latestMediaHandler.movingpicture.latest[1,2,3].year
 #latestMediaHandler.movingpicture.latest[1,2,3].id
 #latestMediaHandler.movingpicture.latest[1,2,3].plot
 #latestMediaHandler.movingpicture.latest[1,2,3].new

* MyFilms
 #latestMediaHandler.myfilms.label
 #latestMediaHandler.myfilms.latest.enabled
 #latestMediaHandler.myfilms.hasnew
 #latestMediaHandler.myfilms.latest[1,2,3].poster
 #latestMediaHandler.myfilms.latest[1,2,3].fanart
 #latestMediaHandler.myfilms.latest[1,2,3].title
 #latestMediaHandler.myfilms.latest[1,2,3].dateAdded
 #latestMediaHandler.myfilms.latest[1,2,3].rating
 #latestMediaHandler.myfilms.latest[1,2,3].roundedRating
 #latestMediaHandler.myfilms.latest[1,2,3].year
 #latestMediaHandler.myfilms.latest[1,2,3].id
 #latestMediaHandler.myfilms.latest[1,2,3].new

* MvCentral:
 #latestMediaHandler.mvcentral.label
 #latestMediaHandler.mvcentral.latest.enabled
 #latestMediaHandler.mvcentral.hasnew
 #latestMediaHandler.mvcentral.latest[1,2,3].thumb
 #latestMediaHandler.mvcentral.latest[1,2,3].artist
 #latestMediaHandler.mvcentral.latest[1,2,3].album
 #latestMediaHandler.mvcentral.latest[1,2,3].track
 #latestMediaHandler.mvcentral.latest[1,2,3].dateAdded
 #latestMediaHandler.mvcentral.latest[1,2,3].fanart
 #latestMediaHandler.mvcentral.latest[1,2,3].genre
 #latestMediaHandler.mvcentral.latest[1,2,3].new

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
 #latestMediaHandler.tvrecordings.latest[1,2,3,4].new"
</pre>