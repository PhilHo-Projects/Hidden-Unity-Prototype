var buildUrl = "Build";
var loaderUrl = buildUrl + "/BuildWebGL.loader.js";
var config = {
  dataUrl: buildUrl + "/BuildWebGL.data.gz",
  frameworkUrl: buildUrl + "/BuildWebGL.framework.js.gz",
  codeUrl: buildUrl + "/BuildWebGL.wasm.gz",
  memoryUrl: buildUrl + "/",
  symbolsUrl: buildUrl + "/",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "philippeho27",
  productName: "ChatroomWars",
  productVersion: "1.0",
};

var container = document.querySelector("#unity-container");
var canvas = document.querySelector("#unity-canvas");
var loadingCover = document.querySelector("#loading-cover");
var progressBarEmpty = document.querySelector("#unity-progress-bar-empty");
var progressBarFull = document.querySelector("#unity-progress-bar-full");
var spinner = document.querySelector(".spinner");

// Fixed aspect ratio placeholder with default values that will be replaced during build
var defaultWidth = 16;
var defaultHeight = 9;
var canvasAspect = defaultWidth / defaultHeight;

const debouncedResize = debounce(resizeGame, 16); // ~60fps


function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

function resizeGame() {
  // Get the actual viewport dimensions (excludes browser UI elements)
  var windowWidth = window.innerWidth;
  var windowHeight = window.innerHeight;
  
  // Set container to fill viewport
  container.style.width = windowWidth + "px";
  container.style.height = windowHeight + "px";
  
  // Maintain aspect ratio for the canvas
  var containerAspect = windowWidth / windowHeight;
  
  if (containerAspect > canvasAspect) {
    // Container is wider than canvas aspect, height is limiting factor
    canvas.style.width = (windowHeight * canvasAspect) + "px";
    canvas.style.height = windowHeight + "px";
  } else {
    // Container is taller than canvas aspect, width is limiting factor
    canvas.style.width = windowWidth + "px";
    canvas.style.height = (windowWidth / canvasAspect) + "px";
  }
  
  // Center the canvas
  canvas.style.position = "absolute";
  canvas.style.left = (windowWidth - canvas.offsetWidth) / 2 + "px";
  canvas.style.top = (windowHeight - canvas.offsetHeight) / 2 + "px";
}

// Audio context handling
function handleAudioContext() {
    let isInitialized = false;
    
    // Try auto-init
    function tryAutoInit() {
      try {
        if (window.unityInstance && window.unityInstance.Module.WebGLAudioContext) {
          window.unityInstance.Module.WebGLAudioContext.resume()
            .then(() => { isInitialized = true; })
            .catch(() => { showMessage(); });
        } else {
          const tempAudioContext = new (window.AudioContext || window.webkitAudioContext)();
          tempAudioContext.resume()
            .then(() => { isInitialized = true; })
            .catch(() => { showMessage(); });
        }
      } catch (e) {
        showMessage();
      }
    }
    
    // Show tap message
    function showMessage() {
      if (!isInitialized) {
        const msg = document.createElement('div');
        msg.style = "position:absolute; top:10px; left:50%; transform:translateX(-50%); background:rgba(0,0,0,0.7); color:white; padding:10px; border-radius:5px; z-index:9999;";
        msg.textContent = "Tap anywhere to enable audio";
        document.body.appendChild(msg);
        
        // Setup interaction handlers
        ['click', 'touchstart', 'keydown'].forEach(evt => {
          document.addEventListener(evt, function initOnInteraction() {
            if (window.unityInstance && window.unityInstance.Module.WebGLAudioContext) {
              window.unityInstance.Module.WebGLAudioContext.resume()
                .then(() => {
                  isInitialized = true;
                  msg.remove();
                });
            }
            // Remove the event listener after first interaction
            document.removeEventListener(evt, initOnInteraction);
          });
        });
      }
    }
    
    // Try auto-init after Unity loads
    setTimeout(tryAutoInit, 1000);
}

window.addEventListener('resize', resizeGame);
resizeGame(); // Initial sizing

if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
  container.className = "unity-mobile";
  // Avoid right-click/long press menu on mobile
  canvas.addEventListener("contextmenu", function(e) { e.preventDefault(); });
}

loadingCover.style.display = "";

var script = document.createElement("script");
script.src = loaderUrl;
script.onload = () => {
  createUnityInstance(canvas, config, (progress) => {
    spinner.style.display = "none";
    progressBarEmpty.style.display = "";
    progressBarFull.style.width = (100 * progress) + "%";
  }).then((unityInstance) => {
    loadingCover.style.display = "none";
    window.unityInstance = unityInstance;
    
    handleAudioContext();

    setTimeout(resizeGame, 500);
    var checkCount = 0;
    var resizeInterval = setInterval(function() {
      resizeGame();
      checkCount++;
      if (checkCount > 10) clearInterval(resizeInterval);
    }, 500);
  }).catch((message) => {
    alert(message);
  });
};
document.body.appendChild(script);

document.addEventListener('DOMContentLoaded', function() {
    // Only for mobile
    if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
      // Allow pinch zoom
      document.querySelector('meta[name="viewport"]').setAttribute(
        'content', 
        'width=device-width, initial-scale=1.0, user-scalable=yes, viewport-fit=cover'
      );
      
      // Track zoom level
      let initialDistance = 0;
      let currentZoom = 1.0;
      let maxZoom = 2.5;
      let minZoom = 1.0;
      
      canvas.addEventListener('touchstart', function(e) {
        if (e.touches.length === 2) {
          initialDistance = Math.hypot(
            e.touches[0].pageX - e.touches[1].pageX,
            e.touches[0].pageY - e.touches[1].pageY
          );
        }
      });
      
      canvas.addEventListener('touchmove', function(e) {
        if (e.touches.length === 2) {
          // Calculate current distance
          const currentDistance = Math.hypot(
            e.touches[0].pageX - e.touches[1].pageX,
            e.touches[0].pageY - e.touches[1].pageY
          );
          
          // Calculate new zoom level
          if (initialDistance > 0) {
            const newZoom = currentZoom * (currentDistance / initialDistance);
            currentZoom = Math.min(Math.max(newZoom, minZoom), maxZoom);
            
            // Apply zoom to canvas immediately
            canvas.style.transform = `scale(${currentZoom})`;
            
            // Center the zoom around the midpoint of the two touches
            const midX = (e.touches[0].pageX + e.touches[1].pageX) / 2;
            const midY = (e.touches[0].pageY + e.touches[1].pageY) / 2;
            
            const canvasRect = canvas.getBoundingClientRect();
            const canvasCenterX = canvasRect.left + canvasRect.width / 2;
            const canvasCenterY = canvasRect.top + canvasRect.height / 2;
            
            const offsetX = (midX - canvasCenterX) * (1 - currentZoom);
            const offsetY = (midY - canvasCenterY) * (1 - currentZoom);
            
            canvas.style.transform = `scale(${currentZoom}) translate(${offsetX/currentZoom}px, ${offsetY/currentZoom}px)`;
            
            // **ADD THIS LINE** - Update canvas sizing in real-time
            debouncedResize();
            
            e.preventDefault();
          }
        }
      });
      
      // Add touchend event to update initialDistance for next pinch gesture
      canvas.addEventListener('touchend', function(e) {
        if (e.touches.length < 2) {
          initialDistance = 0; // Reset for next pinch
        }
      });
    }
});
  
window.addEventListener('orientationchange', function() {
setTimeout(resizeGame, 100);
});

window.addEventListener('scroll', function() {
setTimeout(resizeGame, 100);
});
