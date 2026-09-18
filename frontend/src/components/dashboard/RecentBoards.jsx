import PropTypes from 'prop-types';
import { Layout, MoreVertical, Pencil, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';
import { useLanguage } from '@/context/LanguageContext';

export default function RecentBoards({ boards, loading, onBoardClick, onCreateBoard, createLabel = 'New Board', onEdit, onDelete }) {
  const { t } = useLanguage();

  return (
    <section className="rounded-2xl border border-border-subtle bg-surface-0 p-5 shadow-sm">
      <div className="mb-5 flex items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-black text-text-main">{t('dashboard.recentBoards')}</h2>
          <p className="text-sm font-medium text-text-muted">Boards you opened or updated recently</p>
        </div>
        <Button type="button" size="sm" onClick={onCreateBoard}>{createLabel}</Button>
      </div>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {[1, 2, 3].map((item) => <div key={item} className="h-36 rounded-xl bg-surface-2 animate-pulse" />)}
        </div>
      ) : boards.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-border-subtle bg-surface-1 p-8 text-center">
          <Layout className="mx-auto text-text-subtle" size={28} />
          <p className="mt-3 font-bold text-text-main">{t('dashboard.noBoards')}</p>
          <p className="mt-1 text-sm text-text-muted">{t('dashboard.noBoardsDesc')}</p>
          <Button type="button" variant="outline" className="mt-5" onClick={onCreateBoard}>{createLabel}</Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {boards.slice(0, 6).map((board) => (
            <BoardTile key={board.id} board={board} onClick={onBoardClick} onEdit={onEdit} onDelete={onDelete} />
          ))}
        </div>
      )}
    </section>
  );
}

function BoardTile({ board, onClick, onEdit, onDelete }) {
  const { t } = useLanguage();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);

  useEffect(() => {
    const handler = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) setMenuOpen(false);
    };
    if (menuOpen) document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [menuOpen]);

  return (
    <article
      className="group relative cursor-pointer rounded-2xl border border-border-subtle bg-surface-1 p-4 transition-all hover:border-primary/40 hover:bg-surface-0"
      onClick={() => onClick(board.id)}
    >
      <div className="mb-4 flex items-start justify-between gap-3">
        <div className="grid h-11 w-11 place-items-center rounded-xl text-white shadow-sm" style={{ backgroundColor: board.color || '#6366f1' }}>
          <Layout size={19} />
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="primary">{t('dashboard.active')}</Badge>
          <div className="relative" ref={menuRef}>
            <button
              type="button"
              onClick={(event) => {
                event.stopPropagation();
                setMenuOpen((value) => !value);
              }}
              className="rounded-lg p-1.5 text-text-subtle opacity-0 transition-all hover:bg-hover-bg hover:text-text-main group-hover:opacity-100"
              aria-label="Board actions"
            >
              <MoreVertical size={16} />
            </button>
            {menuOpen && (
              <div className="absolute right-0 top-full z-20 mt-1 w-44 overflow-hidden rounded-xl border border-border-subtle bg-surface-0 shadow-lg">
                <button
                  type="button"
                  onClick={(event) => {
                    setMenuOpen(false);
                    onEdit(event, board);
                  }}
                  className="flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm font-medium text-text-main transition-colors hover:bg-hover-bg"
                >
                  <Pencil size={14} className="text-primary" />
                  {t('dashboard.editBoard')}
                </button>
                <button
                  type="button"
                  onClick={(event) => {
                    setMenuOpen(false);
                    onDelete(event, board);
                  }}
                  className="flex w-full items-center gap-3 px-4 py-2.5 text-left text-sm font-medium text-rose-500 transition-colors hover:bg-rose-50 dark:hover:bg-rose-950/30"
                >
                  <Trash2 size={14} />
                  {t('dashboard.deleteBoard')}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
      <h3 className="truncate text-base font-black text-text-main transition-colors group-hover:text-primary">{board.name}</h3>
      <p className="mt-1 text-xs font-medium text-text-muted">
        {board.updatedAt
          ? t('dashboard.updatedAt', { date: new Date(board.updatedAt).toLocaleDateString() })
          : t('dashboard.createdAt', { date: new Date(board.createdAt).toLocaleDateString() })}
      </p>
    </article>
  );
}

RecentBoards.propTypes = {
  boards: PropTypes.array,
  loading: PropTypes.bool,
  onBoardClick: PropTypes.func.isRequired,
  onCreateBoard: PropTypes.func.isRequired,
  createLabel: PropTypes.string,
  onEdit: PropTypes.func.isRequired,
  onDelete: PropTypes.func.isRequired,
};

BoardTile.propTypes = {
  board: PropTypes.object.isRequired,
  onClick: PropTypes.func.isRequired,
  onEdit: PropTypes.func.isRequired,
  onDelete: PropTypes.func.isRequired,
};
