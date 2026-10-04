import { useLayoutEffect, useRef, useState } from 'react';
import { GoogleLogin } from '@react-oauth/google';

// Google renders an iframe; pass its measured container width rather than "100%".
export default function ResponsiveGoogleLogin(props) {
  const container = useRef(null);
  const [width, setWidth] = useState(200);
  useLayoutEffect(() => {
    const measure = () => {
      const available = container.current?.getBoundingClientRect().width;
      if (available) setWidth(Math.max(200, Math.min(400, Math.floor(available))));
    };
    measure();
    if (typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(measure);
    observer.observe(container.current);
    return () => observer.disconnect();
  }, []);
  return <div ref={container} className="min-w-0 w-full"><GoogleLogin {...props} width={String(width)} /></div>;
}
