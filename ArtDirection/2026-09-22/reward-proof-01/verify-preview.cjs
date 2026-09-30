// Run with Playwright available on NODE_PATH; serves only as a browser-artifact check.
const {chromium} = require('playwright');
const path = require('node:path');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({headless:true,executablePath:process.env.ART_PROOF_CHROMIUM || undefined});
  try {
    const page = await browser.newPage({viewport:{width:1280,height:1450},deviceScaleFactor:1});
    const errors=[];
    page.on('pageerror',error=>errors.push(error.message));
    page.on('response',response=>{if(response.status()>=400)errors.push(response.status()+' '+response.url());});
    await page.goto('http://127.0.0.1:8342/2026-09-22/reward-proof-01/');
    await page.getByRole('status').filter({hasText:'18 örnek ödül'}).waitFor();
    await page.screenshot({path:path.join(__dirname,'preview-desktop.png'),fullPage:true});
    await page.getByRole('button',{name:'Yakındaki ödülü topla'}).click();
    if(!(await page.locator('#collect').isDisabled())) throw Error('Local pickup should consume the near sample.');
    await page.waitForFunction(()=>drops.filter(d=>d.state==='gone').length===1);
    await page.getByRole('button',{name:'Kat sonu uçuşu'}).click();
    await page.waitForFunction(()=>drops.every(d=>d.state==='gone'));
    if(!(await page.locator('#sweep').isDisabled())) throw Error('Empty floor sweep should be disabled.');
    await page.getByRole('button',{name:'Yeniden yerleştir'}).click();
    await page.locator('#density').selectOption('36');
    await page.locator('#radius').check();
    await page.locator('#gray').check();
    await page.screenshot({path:path.join(__dirname,'preview-density-grayscale.png'),fullPage:true});
    await page.locator('#gray').uncheck();
    await page.locator('#radius').uncheck();
    await page.locator('#density').selectOption('18');
    await page.setViewportSize({width:390,height:844});
    await page.screenshot({path:path.join(__dirname,'preview-mobile.png'),fullPage:true});
    if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth)) throw Error('Mobile horizontal overflow.');
    await page.emulateMedia({reducedMotion:'reduce'});
    await page.getByRole('button',{name:'Kat sonu uçuşu'}).click();
    if(!(await page.evaluate(()=>drops.every(d=>d.state==='gone')))) throw Error('Reduced motion should clear without flight.');
    if(errors.length) throw Error(errors.join('\n'));
    const result={status:'passed',checks:['source/scene loads','nearby pickup only','floor sweep completion','reset','36-drop grayscale and radius review','390px no horizontal overflow','reduced-motion sweep','no page or HTTP errors'],screenshots:['preview-desktop.png','preview-density-grayscale.png','preview-mobile.png'],limits:'Browser presentation only; no Unity import, scene-depth, phone or gameplay parity verification.'};
    fs.writeFileSync(path.join(__dirname,'browser-verification.json'),JSON.stringify(result,null,2)+'\n');
    console.log(JSON.stringify(result));
  } finally {await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
