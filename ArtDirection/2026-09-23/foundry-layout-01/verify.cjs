const {chromium}=require('playwright');
(async()=>{const b=await chromium.launch({executablePath:'C:/Users/pc/AppData/Local/ms-playwright/chromium-1243/chrome-win64/chrome.exe',headless:true});
const page=await b.newPage({viewport:{width:1280,height:1000}});let errors=[];page.on('pageerror',e=>errors.push(e.message));
await page.goto('http://127.0.0.1:8342/2026-09-23/foundry-layout-01/');await page.locator('.concept').evaluate(i=>i.decode());
for(const id of ['entry','square','salvage','pass','gate']){await page.locator('[data-place="'+id+'"]').click();if(await page.locator('[data-place="'+id+'"]').getAttribute('aria-pressed')!=='true')throw Error('Selection failed');}
await page.locator('#projection').click();await page.locator('[data-place="square"]').click();
const point=await page.evaluate(()=>{const r=map.getBoundingClientRect(),v=map._view,q=project([0,-15],v.isIso);return {x:r.left+(map.width/2+(q[0]-v.focus[0])*v.scale)/map.width*r.width,y:r.top+(map.height/2+(q[1]-v.focus[1])*v.scale)/map.height*r.height}});
await page.mouse.click(point.x,point.y);const pos=await page.evaluate(()=>hero);if(Math.abs(pos[0])>.1||Math.abs(pos[1]+15)>.1)throw Error('Map pointer conversion failed');
await page.screenshot({path:'TestResults/foundry-layout-desktop.png',fullPage:true});
await page.setViewportSize({width:390,height:844});await page.screenshot({path:'TestResults/foundry-layout-mobile.png',fullPage:true});
const state=await page.evaluate(()=>({overflow:document.documentElement.scrollWidth>innerWidth,images:[...document.images].every(i=>i.complete&&i.naturalWidth>0)}));
if(state.overflow||!state.images||errors.length)throw Error(JSON.stringify({state,errors}));console.log(JSON.stringify({selection:5,pointer:'passed',...state,errors}));await b.close();})();
