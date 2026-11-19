'use client';
import { useEffect } from 'react';
export default function GameRuntime(){
 useEffect(()=>{
   if(document.querySelector('script[data-game-runtime]'))return;
   const script=document.createElement('script');
   script.type='module';
   script.src='/game.mjs?v=cannon-dot-cleanup140';
   script.dataset.gameRuntime='true';
   document.body.appendChild(script);
 },[]);
 return null;
}
