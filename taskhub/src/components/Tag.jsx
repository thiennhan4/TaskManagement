export default function Tag({ text }) {
  return (
    <div className="flex items-center gap-1.5 text-gray-500 text-sm">
      <div className="w-1.5 h-1.5 rounded-full bg-violet-500"></div>
      {text}
    </div>
  );
}