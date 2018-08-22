//This is the "Offline copy of pages" service worker
var CACHE_NAME = "dtmf-cache";


//Install stage sets up the index page (home page) in the cache and opens a new cache
self.addEventListener('install', function (event) {
    //rather than gracefully waiting for the tab to die, just push the old one down a pit and use this one instead
    //https://developers.google.com/web/fundamentals/primers/service-workers/lifecycle#skip_the_waiting_phase
    self.skipWaiting();

    var indexPage = new Request('/');
    event.waitUntil(
        fetch(indexPage).then(function (response) {
            return caches.open(CACHE_NAME).then(function(cache) {
                console.log('SW - cached index page during Install' + response.url);
                return cache.put(indexPage, response);
            });
        }));
});

//If any fetch fails, it will look for the request in the cache and serve it from there first
self.addEventListener('fetch', function (event) {

    event.respondWith(fetch(event.request).then(function(response) {
        // Check if we received a valid response
        if (!response || response.status !== 200 || response.type !== 'basic') {
            return response;
        }

        // IMPORTANT: Clone the response. A response is a stream
        // and because we want the browser to consume the response
        // as well as the cache consuming the response, we need
        // to clone it so we have two streams.
        var responseToCache = response.clone();

        caches.open(CACHE_NAME)
            .then(function (cache) {
                cache.put(event.request, responseToCache);
            });

        return response;
    }).catch(function (error) {
        console.log('SW - Network request Failed. Serving content from cache: ' + error);

        //Check to see if you have it in the cache
        //Return response
        //If not in the cache, then return error page
        return caches.open(CACHE_NAME).then(function (cache) {
            return cache.match(event.request).then(function (matching) {
                var report = !matching || matching.status == 404 ? Promise.reject('no-match') : matching;
                return report;
            });
        });
    }));
});