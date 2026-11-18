import type {Metadata,Viewport} from 'next';
import './globals.css';
export const viewport:Viewport={width:'device-width',initialScale:1,viewportFit:'cover'};
export const metadata:Metadata={title:'그건 님이!!! · 마왕군 탈출 작전',description:'마부와 포수가 흔들리는 마차에서 왕국군을 따돌리는 최대 4인 온라인 협동 게임',icons:{icon:'/favicon.svg'}};
export default function Layout({children}:{children:React.ReactNode}){return <html lang="ko"><body>{children}</body></html>}
