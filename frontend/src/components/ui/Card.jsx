import React from 'react';
import PropTypes from 'prop-types';

const Card = ({ children, className = '', padding = 'p-6', hover = true }) => {
  return (
    <div className={`
      premium-card 
      ${padding} 
      ${hover ? 'hover:-translate-y-1' : ''} 
      ${className}
    `}>
      {children}
    </div>
  );
};

const CardHeader = ({ children, className = '' }) => (
  <div className={`mb-4 flex items-center justify-between ${className}`}>
    {children}
  </div>
);

const CardTitle = ({ children, className = '' }) => (
  <h3 className={`text-lg font-bold text-text-main ${className}`}>
    {children}
  </h3>
);

const CardDescription = ({ children, className = '' }) => (
  <p className={`text-sm text-text-muted mt-1 ${className}`}>
    {children}
  </p>
);

const CardContent = ({ children, className = '' }) => (
  <div className={className}>
    {children}
  </div>
);

const CardFooter = ({ children, className = '' }) => (
  <div className={`mt-6 pt-6 border-t border-border-subtle ${className}`}>
    {children}
  </div>
);

Card.Header = CardHeader;
Card.Title = CardTitle;
Card.Description = CardDescription;
Card.Content = CardContent;
Card.Footer = CardFooter;

Card.propTypes = {
  children: PropTypes.node.isRequired,
  className: PropTypes.string,
  padding: PropTypes.string,
  hover: PropTypes.bool,
};

export default Card;
export { CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
