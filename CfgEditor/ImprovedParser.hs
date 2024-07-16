{-# LANGUAGE TupleSections #-}
{-# LANGUAGE LambdaCase #-}
module ImprovedParser where
import qualified Data.ByteString.Char8 as C
import qualified Text.Hex as TH
import qualified Data.Text as DT
import qualified Data.ByteString.Lazy as BS
import qualified Data.ByteString as BSS
import Data.Text.Encoding (decodeUtf8)
import Control.Applicative (Alternative(..))
import Control.Monad (replicateM)
import Data.Bits (shiftL)
import Data.Char (isDigit, isHexDigit, isSpace, chr, ord, digitToInt,isControl)
import Data.Functor (($>))
import Data.List (intercalate)
import Data.List.Split (split, dropFinalBlank, keepDelimsR, onSublist)
import qualified Data.List.NonEmpty as NEL
import GHC.Generics (Generic)
import Numeric (showHex)
import Prelude hiding (lines)
import Text.Printf (printf)
import System.Directory (getAppUserDataDirectory)
import System.IO (openFile, IOMode (ReadMode), hGetContents, hClose)
import System.Environment (getArgs)
import Trace.Hpc.Util (writeFileUtf8)
import Data.List.Split.Internals (Chunk(Text))

type RootName = String
type Value = String
data Tree = Node (RootName,Value) [Tree]
instance Show Tree where show t = showTree t 0
tabNode (Node _ []) = ""
tabNode (Node (root,value) _) = "\t"

fixRootName (rootName,[]) = rootName
fixRootName (rootName,value)= rootName++" = " ++ "\'"++value++"\'"


showTree::Tree->Int->String
showTree (Node ("Root",[]) []) i = "Root = ''"
showTree (Node (varname,[]) []) i = tabs ++ varname ++"\n"
  where tabs =replicate i '\t'
showTree (Node (varname,value) []) i = tabs++varname++" = "++"'"++value++"'\n"
  where tabs =replicate i '\t'
showTree (Node rootName ls) 0 = fixRootName rootName++" {\n"++concatMap (\x-> tabNode x ++showTree x 1 ) ls ++"}\n"
showTree (Node rootName ls) i = initN tabs++fixRootName rootName++" {\n"++concatMap (\x-> tabNode x ++showTree x (i+1) ) ls ++tabs++"}\n"
  where tabs =replicate i '\t'




data ParseResult a = Error [String] | Result a
newtype Parser i o = Parser {
    runParser_ :: TextZipper i -> ParseResult (TextZipper i, o)
  }
runParser :: Parser String o -> String -> ParseResult (String, o)
runParser parser input =
  case runParser_ parser (textZipper $ lines input) of
    Error errs             -> Error errs
    Result (restZ, output) -> Result (leftOver restZ, output)
  where
    leftOver tz = concat (tzRight tz : tzBelow tz)
data TextZipper a =
  TextZipper {
    tzLeft  :: a
  , tzRight :: a
  , tzAbove :: [a]
  , tzBelow :: [a]
  }

instance Show a => Show (ParseResult a) where
  show (Result res) = show res
  show (Error errs) = formatErrors (reverse errs)
    where
      formatErrors []         = error "No errors to format"
      --formatErrors [err]      = err
      formatErrors (err:errs) = err
      delim = "\n-> "
      padNewline '\n' = '\n':replicate (length delim - 1) ' '
      padNewline c    = [c]

instance Functor ParseResult where
  fmap _ (Error errs) = Error errs
  fmap f (Result res) = Result (f res)

instance Applicative ParseResult where
  pure = Result
  Error  errs <*> _   = Error errs
  Result f <*> result = fmap f result
instance Functor (Parser i) where
  fmap f parser = Parser $ fmap (fmap f) . runParser_ parser

instance Applicative (Parser i) where
  pure x    = Parser $ pure . (, x)
  pf <*> pa = Parser $ \input -> case runParser_ pf input of
    Error err        -> Error err
    Result (rest, f) -> fmap f <$> runParser_ pa rest

instance Monad (Parser i) where
  parser >>= f = Parser $ \input -> case runParser_ parser input of
    Error err        -> Error err
    Result (rest, o) -> runParser_ (f o) rest

instance Show a => Show (TextZipper a) where
  show (TextZipper left right above below) =
    "TextZipper{left=" <> show left
      <> ", right=" <> show right
      <> ", above=" <> show above
      <> ", below=" <> show below
      <> "}"

textZipper :: [String] -> TextZipper String
textZipper []           = TextZipper "" "" [] []
textZipper (first:rest) = TextZipper "" first [] rest

currentPosition :: TextZipper String -> (Int, Int)
currentPosition zipper =
  (length (tzAbove zipper) + 1, length (tzLeft zipper) + 1)

currentChar :: TextZipper String -> Maybe Char
currentChar zipper = case tzRight zipper of
  []    -> Nothing
  (c:_) -> Just c

lines :: String -> [String]
lines = (split . dropFinalBlank . keepDelimsR . onSublist) "\n"

addPosition :: String -> TextZipper String -> String
addPosition err zipper =
  let (ln, cn) = currentPosition zipper
      err'     = printf (err <> " at line %d, column %d: ") ln cn
      left     = reverse $ tzLeft zipper
      right    = tzRight zipper
      left'    = showStr $ drop (length left - ctxLen) left
      right'   = showStr $ take ctxLen right
      line     = left' <> right'
  in printf (err' <> "%s\n%s^")
            line
            (replicate (length err' + length left') ' ')
  where
    ctxLen = 6
    showStr = concatMap showCharForErrorMsg

showCharForErrorMsg :: Char -> String
showCharForErrorMsg c = case c of
  ',' -> ""
  '\b' -> ""
  '\f' -> ""
  '\n' -> ""
  '\r' -> ""
  '\t' -> ""
  ' '  -> " "
  _ | isControl c -> "\\" <> show (ord c)
  _ -> [c]

parseError :: String -> TextZipper String -> ParseResult a
parseError err zipper = Error [addPosition err zipper]

throw :: String -> Parser String o
throw = Parser . parseError

elseThrow :: Parser String o -> String -> Parser String o
elseThrow parser err = Parser $ \input ->
  case runParser_ parser input of
    Result (rest, a) -> Result (rest, a)
    Error errs       -> Error (addPosition err input : errs)

moveByOne :: TextZipper String -> TextZipper String
moveByOne zipper
  -- not at end of line
  | not $ null (tzRight zipper) =
      zipper { tzLeft  = head (tzRight zipper) : tzLeft zipper
             , tzRight = tail $ tzRight zipper
             }
  -- at end of line but not at end of input
  | not $ null (tzBelow zipper) =
      zipper { tzAbove = tzLeft zipper : tzAbove zipper
             , tzBelow = tail $ tzBelow zipper
             , tzLeft  = ""
             , tzRight = head $ tzBelow zipper
             }
  -- at end of input
  | otherwise = zipper

move :: TextZipper String -> TextZipper String
move zipper = let zipper' = moveByOne zipper
  in case currentChar zipper' of
       Just _  -> zipper'
       Nothing -> moveByOne zipper'
lookahead :: Parser String Char
lookahead = Parser $ \input -> case currentChar input of
  Just c  -> Result (input, c)
  Nothing -> parseError "Empty input" input

safeLookahead :: Parser String (Maybe Char)
safeLookahead = Parser $ \input -> case currentChar input of
  Just c  -> Result (input, Just c)
  Nothing -> Result (input, Nothing)

satisfy :: (Char -> Bool) -> String -> Parser String Char
satisfy predicate expectation = Parser $ \input -> case currentChar input of
  Just c | predicate c -> Result (move input, c)
  Just c               -> flip parseError input $
    expectation <> ", got '" <> showCharForErrorMsg c <> "'"
  _                    -> flip parseError input $
    expectation <> ", but the input is empty"

char :: Char -> Parser String Char
char c = satisfy (== c) $ printf "Expected '%v'" $ showCharForErrorMsg c

digit :: Parser String Int
digit = digitToInt <$> satisfy isDigit "Expected a digit"

string :: String -> Parser String String
string ""     = pure ""
string (c:cs) = (:) <$> char c <*> string cs

surroundedBy :: Parser i a -> Parser i b -> Parser i a
surroundedBy parser1 parser2 = parser2 *> parser1 <* parser2

separatedBy :: Parser String v -> Char -> String -> Parser String [v]
separatedBy parser sepChar errMsg = do
  res <- parser  `elseThrow` errMsg
  safeLookahead >>= \case
    Just c | c == sepChar ->
      (res:) <$> (char sepChar *> separatedBy parser sepChar errMsg)
    _ -> return [res]

oneOf1 s = satisfy (`elem` s) set
spaces :: Parser String String
spaces = safeLookahead >>= \case
  Just c | isWhitespace c -> (:) <$> char c <*> spaces
  _                       -> return ""
  where
    isWhitespace c = c == ' ' || c == '\n' || c == '\r' || c == '\t'
--dada
set = "Allowed symbols: "++['a'..'z']++['A'..'Z']++[':','$','#','(',')','[',']','_','/','-']++['0'..'9']
varname:: Parser String String
varname = do
  c <- lookahead `elseThrow` "Expected rest of a string"
  case c of
    ' ' -> "" <$ char ' '
    '\n'-> "" <$ char ' '
    '\t'-> "" <$ char '\t'
    _ -> (:) <$> oneOf1 set <*> varname --
setBom = "я╗┐"
parseBom:: Parser String String
parseBom = do
  c <- lookahead `elseThrow` "Expected rest of a string"
  case c of
    '┐' -> "┐" <$ char '┐'
    _  -> (:) <$> oneOf1 setBom <*> parseBom

value'::Parser String String
value' = do
    c <- lookahead `elseThrow` "Expected rest of a string"
    if c=='\''
    then "" <$ char '\''
    else do (:) <$> char c <*> value'
value = char '\'' *> value'
parseTree :: Parser String Tree
parseTree =  do
  spaces
  var <- varname
  spaces
  _ <- char '='
  spaces
  v <- value <* spaces
  _ <- char '{' <* spaces
  c <- lookahead `elseThrow` "Expected a value or '}'"
  case c of
     '}' ->  Node (var,v) [] <$ char '}'
     _   -> do
                s<- separatedBy (spaces *> parseTree  <* spaces) ',' "Expected a variable/class" <* satisfy (== '}') "Expected '}'"
                return $ Node (var,v) s
emptyRow str = all (\x->elem x "\t\n ") str
fixlines [] = []
fixlines str
 |elem '=' str && notElem '{' str && notElem '}' str && filtQuotes == 2 = str++" {}\n"
 |elem '=' str = str++"\n"
 |notElem '=' str && elem '{' str && last (concat . words $ str) == '{'= filt ++" = "++"\'\'"++" {\n"
 |notElem '=' str && elem '{' str && last str /= '{'= str ++" = "++"\'\'"++" {\n"
 |notElem '=' str && notElem '}' str && (all (`notElem` set) str) = "    \\n "
 |notElem '=' str && notElem '}' str = filt ++" = "++"\'\'"++" {}\n"
 |notElem '=' str  && notElem '{' str && notElem '}' str && filtQuotes == 1 = filt ++" {}\n"
 |elem '}' str && (emptyRow $tail (dropWhile (/='}') str)) = let t = (takeWhile (/= '}') str) in t++"}\n"
 |otherwise = str++"\n"
  where
    filt = filter (/= '{') str
    filtQuotes =length $ filter (== '\'') str
    rmw = concat (words filt)
--
initN [] = []
initN str = init str
fixSeparator :: String -> String -> Bool
fixSeparator [] _ = False
fixSeparator _ [] = False
fixSeparator str1 str2 = elem '}' str1 && elem '}' str2 && check1== head check2 && check1 == '}'
  where
    check1 = last (initN str1)
    check2 = filter (not . isSpace) str2
fixSeparation :: [String] -> [String]
fixSeparation [] = []
fixSeparation [x] = [x]
fixSeparation (x:y:more)
 |fixSeparator x y = x:fixSeparation (y:more)
 |elem '}' x && (not $ emptyRow y) && (not (null y))= (initN x++",\n"):fixSeparation (y:more)
 |otherwise = x:fixSeparation (y:more)
empty1 :: String -> Bool
empty1 "" = True
empty1 _ = False
format1 str =initN $ concat$ fixSeparation $ map (fixlines . removeLastN . filter (/='\r')) (lines str)
(Just bom) =  TH.decodeHex $ DT.pack "efbbbf" --
parsePath::FilePath->IO (Tree,Bool)
parsePath path =
  do
     --[path]<-getArgs
     contents<-BSS.readFile path
     let c =  BSS.take 3 contents
     let isBom = bom == c
     if isBom then 
        do
          contents1h <- openFile path ReadMode
          contents11 <- hGetContents contents1h
          let contents1 = drop 3 contents11
          let root = concat . words $ take 4 contents1
          if root /= "Root" then do
             putStrLn $ "Invalid tree structure - Root node wrapper expected, instead got \"" ++ root ++  "\" at start"
             hClose contents1h
             return (Node ("грешка","") [],True)
          else do 
            let formated = format1 contents1
            let save = runParser parseTree formated
            case save of
              (Result (s,d)) -> if all (`elem` "\n\t\r ") s || null s then 
                do 
                    hClose contents1h 
                    return (d,isBom) 
                else 
                      do 
                          putStrLn $ "There are elements or multiple \\n at line: "++show (length $ (lines . show) d)++" after Root {} class"
                          hClose contents1h
                          return (Node ("грешка","") [],isBom)
              _ -> do 
                    putStrLn $show save
                    return (Node ("грешка","") [],isBom)
      else do
       c2 <- openFile path ReadMode 
       contents3 <- hGetContents c2
       let numLines = length $ lines contents3
       let formated = format1 contents3
       let a= runParser parseTree formated
       let root = concat . words $ take 4 contents3
       if root /= "Root" then do
            putStrLn $ "Invalid tree structure - Root node wrapper expected, instead got \"" ++ root ++  "\" at start"
            hClose c2
            return (Node ("грешка","") [],True)
       else do
              case a of
                (Result (s,d)) ->if all (`elem` "\n\t\r ") s || null s 
                  then do 
                  hClose c2 
                  return (d,isBom) 
                  else do
                    putStrLn $  "There are elements or multiple \\n at line: "++show (length $ (lines . show) d)++" after Root {} class"
                    hClose c2
                    return (Node ("грешка","") [],isBom)
                _ -> do 
                  putStrLn $show a
                  hClose c2
                  return (Node ("грешка","") [],isBom)

removeLastN [] = []
removeLastN str = if last str == '\n' then initN str else str
tests = do
     path <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
     tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
     c2 <- openFile path ReadMode
     contents3 <- hGetContents c2
     let save1 = runParser parseTree $ format1 contents3
     let a= runParser parseTree (format1 contents3)
     putStrLn $  format1 contents3
     putStrLn  $show a
     return ()--
