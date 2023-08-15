{-# LANGUAGE ScopedTypeVariables #-}

import Control.Monad
import Control.Applicative
import System.IO
import Data.List.Split (splitOn)
import System.Environment
import System.Directory
import Data.Char (isAsciiLower, isAsciiUpper, isDigit,toUpper,isUpper,isLower)
import Control.Exception
import System.Exit

newtype Parser a = Parser { parse :: String -> [(a,String)] }
--Basic bind for parsers
bind :: Parser a -> (a -> Parser b) -> Parser b
bind p f = Parser (concatMap (\(a, s') -> parse (f a) s') . parse p)
--Unit mainly used for type convert (a->Parser a)
unit :: a -> Parser a
unit a = Parser (\s -> [(a,s)])

--taketoken , useful to apply parser to a string and just return the parsed value
taketoken::Parser a->String->a
taketoken p str = fst (head (parse p str))
--takerest , ignores the token and just returns whats left of a string after parsing
takerest::Parser a->String ->String
takerest p str = snd (head (parse p str))

{-
Parsers are instances of certain typeclasses
-}
instance Functor Parser where
  fmap f (Parser cs) = Parser (\s -> [(f a, b) | (a, b) <- cs s])

instance Applicative Parser where
  pure = return
  (Parser cs1) <*> (Parser cs2) = Parser (\s -> [(f a, s2) | (f, s1) <- cs1 s, (a, s2) <- cs2 s1])

instance Monad Parser where
  return = unit
  (>>=)  = bind

instance MonadPlus Parser where
  mzero = failure
  mplus = combine

instance Alternative Parser where
  empty = mzero
  (<|>) = option

--Single Char parsing
item :: Parser Char
item = Parser (\s ->
  case s of
   []     -> []
   (c:cs) -> [(c,cs)])

--Name not to be confused with Data.List.Split functions
--oneOf1 takes a string [Char] and parses a single char that is from the string
oneOf1 :: String -> Parser Char
oneOf1 s = satisfy (`elem` s)
--Satisfy creates parser of char, only if that char satisfies the predicate p
satisfy :: (Char -> Bool) -> Parser Char
satisfy p = bind item (\c ->
  if p c
  then unit c
  else Parser (const []))
--combines two parsers
combine :: Parser a -> Parser a -> Parser a
combine p q = Parser (\s -> parse p s ++ parse q s)
--fails on any input
failure :: Parser a
failure = Parser (const [])
-- if can parse p then parse p if can parse q parse q
option :: Parser a -> Parser a -> Parser a
option  p q = Parser $ \s ->
  case parse p s of
    []     -> parse q s
    res    -> res
--like item but can isolate char as par
char :: Char -> Parser Char
char c = satisfy (c ==)

--parses the string
string :: String -> Parser String
string [] = return []
string (c:cs) = do { char c; string cs; return (c:cs)}
--token space eater
spaces :: Parser String
spaces = many (oneOf1 "' '\n\r\\\b\t")

token :: Parser a -> Parser a
token p = do
  {
    a <- p;
    spaces ;
    return a
    }

reserved :: String -> Parser String
reserved s = token (string s)

digit :: Parser Char
digit = satisfy isDigit

uscore :: Parser Char
uscore = satisfy (=='_')


lower::Parser Char
lower = satisfy isLower

upper::Parser Char
upper = satisfy isUpper

letter::Parser Char
letter = combine lower upper

alphanum::Parser Char
alphanum =  letter <|> digit
--integrated seq for some reason didn't work as intended,so I rework it
seqStr::Parser String -> Parser String-> Parser String
seqStr p q = bind p (\x-> bind q (\xs-> unit (x++xs)))

sepBy1::Parser a->Parser b->Parser [b]
sepBy1 sep element = (:) <$> element <*> many (sep *> element) <|> pure []


validData::[(a,String)]->Bool
validData [(a,"")] = True
validData _ = False

--Settings.cfg paraworld
type VarName = String
type Value = String
type RootName = String
data Tree = CV VarName Value | Node RootName [Tree] --deriving (Show)
instance Show Tree where
 show t = showTree t 0


tabNode (CV _ _ ) = []
tabNode (Node _ _) = "\t"
showTree::Tree->Int->String
showTree (CV varname value) i = tabs++varname++" = "++"'"++value++"'\n"
  where tabs =replicate i '\t'
showTree (Node rootName ls) 0 = rootName++" {\n"++concatMap (\x-> tabNode x ++showTree x 1 ) ls ++"}\n"
showTree (Node rootName ls) i = init tabs++rootName++" {\n"++concatMap (\x-> tabNode x ++showTree x (i+1) ) ls ++tabs++"}\n"
  where tabs =replicate i '\t'
--Paraworldstuffparse
var :: Parser [Char]
var = many $ alphanum <|> uscore
parseNull::Parser Tree
parseNull =
  do
     varname <- var
     reserved "=''"
     return (CV varname "")

parseCV::Parser Tree
parseCV =
  do
    parseNull
    <|>
    do
    varname <- var
    spaces
    reserved "="
    spaces
    s <- many $ satisfy (/= '\'')
    char '\''
    return (CV varname s)


parseTree::Parser Tree
parseTree =
  do
     root <- var
     reserved "{"
     --ls<- parseCV
     ls <-sepBy1 (spaces *> char '|' <* spaces) parseTree
     reserved "}"
     return  (Node root ls)
   <|>
   parseCV



tryExtract str = if null datas then (CV "Parse Error" "")
 else fst . head $ datas
 where
      datas = parse parseTree str

alphanumeric arg = any (\x->x arg) [isDigit,isAsciiLower,isAsciiUpper]
 --spacenewlinetrick pls dont write inside the cfg urself
modifySymbol :: Char -> Bool -> [Char] -> [Char]
modifySymbol _ b [] = []
modifySymbol _ b [x]  = [x]
modifySymbol s b (x:y:xs)
 |(x=='\'') && alphanumeric y && b = x:s:modifySymbol s False (y:xs)
 |(x=='\'') && alphanumeric y = x:y:modifySymbol s True (xs)
 |x=='}' && alphanumeric y  = x:s:modifySymbol s b (y:xs)
 |x=='\'' = x:modifySymbol s (not b) (y:xs)
 |otherwise = x:modifySymbol s b (y:xs)
iotest::Bool->String->IO String
iotest b hpath =
  do
     tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
     check <- checkMonadSpam hpath
     handle <- openFile hpath ReadMode
     contents <- hGetContents handle
     let filt = modifySymbol '|' False $concat $ words contents
     putStrLn "Parsing..."
     let ptr = tryExtract filt
     if (not$ eqTree ptr (CV "Parse Error" [])) then do
       putStrLn "Parsing Successful"
       if (b) then do
          putStrLn "Commands are:\n Set(path,value) - set argpath argvalue \n Get(path) - get argpath \n Remove(path) - remove path \n Q - exit \n CD path - change directory \n PWD - current directory"
          iotest False hpath
       else do
       arg<-getLine
       if map toUpper arg=="Q" then return "Exit"
       else if map toUpper (head (cdfix arg)) =="CD" then do
        let p = head $ tail $cdfix arg
        putStrLn $ "Command: CD \n| argpath = "++p++"\n"
        check<-checkMonadSingle p
        if check then do
        iotest False p
        else do
          putStrLn "Not a valid path"
          iotest False hpath
       else if map toUpper arg=="PWD" then do
        putStrLn $ "Command: PWD \n |Current Dir:\n"++hpath
        iotest False hpath else do
       let com = trComm arg
       let cs = "-s"
       let get = "-g"
       let rem = "-r"
       if (length com == 3 && head com == cs) then do
          let arg1 = com!!1
          let arg2 = com!!2
          putStrLn $ "Command: Set \n| argpath = "++arg1++"\n| argvalue = "++arg2
          let carg1= convertSet (arg1,arg2)
          let set = addToTree ptr (fst carg1) (snd carg1)
          if strongEqTree set ptr then do
                  putStrLn "Set Failed | Nothing was changed"
                  iotest False hpath
          else do
          (tempName, tempHandle) <- openTempFile tempPath "temp"
          hPutStr tempHandle $ show set
          hClose handle
          hClose tempHandle
          renameFile tempName hpath
          iotest False hpath
        else if (length com == 2) then
            if (head com == get) then do
               putStrLn $ "Command: Get \n| argpath = "++com!!1
               let carg = convertGet $com!!1
               putStrLn $ "Value:"++getTree ptr carg
               iotest False hpath
            else if (head com == rem) then do
               putStrLn $ "Command: Remove \n| argpath = "++com!!1
               let carg2 = convertGet $ com!!1
               let r = deleteTree ptr carg2
               if strongEqTree r ptr then do
                  putStrLn "Remove Failed | Nothing was changed"
                  iotest False hpath
               else
                do
                   (tempName, tempHandle) <- openTempFile tempPath "temp"
                   hPutStr tempHandle $ show r
                   hClose handle
                   hClose tempHandle
                   renameFile tempName hpath
                   iotest False hpath
            else do
              putStrLn $ "Not a valid command with "++show (length com-1)++" arguments"
              iotest False hpath
          else do
            putStrLn $"Not a valid command with "++show (length com-1)++" arguments"
            iotest False hpath
        else
          do
             putStrLn "Parsing Failed"
             return "False"
     --writeFile "Settings1.cfg" $show d

strongEqTree::Tree->Tree->Bool
strongEqTree (CV _ _) (Node _ _) = False
strongEqTree (Node _ _) (CV _ _) = False
strongEqTree (CV n1 t1) (CV n2 t2) = n1==n2 && t1==t2
strongEqTree (Node n1 t1) (Node n2 t2) = n1 == n2 && length t1==length t2 && and (zipWith (strongEqTree) t1 t2)

eqTree (CV _ _) (Node _ _) = False
eqTree (Node _ _) (CV _ _) = False
eqTree (CV n1 _) (CV n2 _) = n1==n2
eqTree (Node n1 t1) (Node n2 t2) = n1 == n2 
addToTree::Tree->[String]->(VarName,Value)->Tree
addToTree y@(Node root tree) [] (vname,vvalue) = y
addToTree y@(Node root tree) [x] (vname,vvalue)
  |root == x = if null search then Node root $tree++[(CV vname vvalue)] else Node root filtered
  |otherwise = y
  where
      search = [(CV n vvalue)|(CV n v)<-tree,vname==n]
      filtered = map (\x->if not$ eqTree x (CV vname "") then x else (CV vname vvalue)) tree
addToTree z@(Node root tree) (x:y:xs) cv@(vname,vvalue)
  |root == x = if null search then addToTree (Node root (tree++[(Node y [])])) (x:y:xs) cv else Node root filtered
  |otherwise = z
   where
      search = [(Node r t)|(Node r t)<-tree,r==y]
      filtered = map (\x->if eqTree x (Node y []) then addToTree x (y:xs) cv else x) tree


convertSet (t,v) = (init split,(l,v))
  where
    split = splitOn "/" t
    l = last split
convertGet  = splitOn "/"

getTree (CV name v) [x]
 |name==x = v
 |otherwise = "Err:No value found"
getTree (Node root _) [x] = "Err:No value found"
getTree (Node root tree) (x:y:xs)
  |root == x && (not$null filtNodes) = getTree (head filtNodes) (y:xs)
  |root == x && (not$null filtLeafs) = getTree (head filtLeafs) (y:xs)
  |otherwise = "Err:No value found"
    where
      filtNodes = [(Node r t)|(Node r t)<-tree,r==y]
      filtLeafs = [(CV n v)|(CV n v)<-tree,n==y]


deleteTree1::Tree->[String]->Tree
deleteTree1 q@(CV _ _ ) _ = q
deleteTree1 q [x] = q 
deleteTree1 a@(Node root tree) [x,y]
  |root == x && (not$null filtNodes) = Node root filteredNodes
  |root == x && (not$null filtLeafs) = Node root filteredLeafs
  |otherwise = a
    where
     filtNodes = [(Node r t) | (Node r t)<-tree,r==y]
     filtLeafs = [(CV n v)|(CV n v)<-tree,n==y]
     filteredNodes = filter (\x->not $eqTree x (Node y [])) tree
     filteredLeafs = filter (\x->not $eqTree x (CV y [])) tree
deleteTree1 a@(Node root tree) (x:y:xs)
 |x == root = Node root (map (\x->deleteTree1 x (y:xs)) tree)
 |otherwise = a

deleteTree::Tree->[String]->Tree
deleteTree q@(CV _ _) _ = q
deleteTree a@(Node root tree) b =Node a1 $map (`deleteTree` b) filt
 where
  filt = filter (not . isEmptyNode) b2
  isEmptyNode::Tree->Bool
  isEmptyNode (Node _ []) = True
  isEmptyNode _ = False
  (Node a1 b2) = deleteTree1 a b

openMonad path = do
    handle (\(e::IOException) -> return Nothing) $ do
      h <- openFile path ReadMode
      return (Just h)
checkMonadSingle :: [Char] -> IO Bool
checkMonadSingle path = do   
     tryopen <- openMonad path
     case tryopen of
      Nothing -> return False
      (Just h) ->do
         hClose h 
         return True

checkMonadSpam :: [Char] -> IO Bool
checkMonadSpam path = do   
     tryopen <- openMonad path
     case tryopen of
      Nothing -> do 
        putStrLn "File not found | Please write a valid path or close the program:"
        p <- getLine 
        checkMonadSpam p 
      (Just h) ->do
         hClose h 
         return True

main:: IO String
main =
  do 
     hcodepath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld\\Settings.cfg"
     tempPath <- getAppUserDataDirectory "SpieleEntwicklungsKombinat\\Paraworld"
     check <- checkMonadSingle hcodepath
     if not check then die "No Settings file found in AppData"
     else do
     args<-getArgs
     handle <- openFile hcodepath ReadWriteMode
     contents <- hGetContents handle
     let filt = modifySymbol '|' False $concat $ words contents
   --putStrLn "Parsing..."
     let ptr = tryExtract filt
     if (not$ eqTree ptr (CV "Parse Error" [])) then
       case args
          of
            com@[command,path,value]
             |elem com [["-s",path,value],["--set",path,value]] ->
             do
               let carg1= convertSet (path,value)
               let set = addToTree ptr (fst carg1) (snd carg1)
               (tempName, tempHandle) <- openTempFile tempPath "temp"
               hPutStr tempHandle $ show set
               hClose handle
               hClose tempHandle
               renameFile tempName hcodepath
               return "Spawned"
              |otherwise -> if elem command ["-s","--set"] then do
                    die "Exit with Code (101) - wrong number of parameters" -- !#par
               else
                die "Exit with Code (102) - Wrong Command"
            com@[command,path]
             |elem com [["-r",path],["--remove",path]] ->
               do
                 let carg2 = convertGet path
                 let r = deleteTree ptr carg2
                 if strongEqTree r ptr then
                  do
                    die "Exit with Code (201) - Nothing was changed" --Remove changed nothing
                 else do
                 (tempName, tempHandle) <- openTempFile tempPath "temp"
                 hPutStr tempHandle $ show r
                 hClose handle
                 hClose tempHandle
                 renameFile tempName hcodepath
                 return "Deded"
             |elem com [["-g",path],["--get",path]] ->
              do
                 let carg = convertGet path
                 if (getTree ptr carg) == "Err:No value found" then do
                  die "Exit with Code (301) - Get returned nothing"-- !vfound
                 else do 
                  putStrLn $ getTree ptr carg
                  return "GetLow"
             |otherwise -> die "Exit with Code (102) - Wrong Command"
            [] -> do
                  putStrLn hcodepath
                  iotest True hcodepath
            _ -> die "Exit with Code (666) No valid arguments"
     else die "Exit with Code (100) - Parse Error"
      


--Set("path","value")
--Remove("path")
--Get("path")

trComm::String->[String]
trComm str = case com of
   "SET" -> "-s":args
   "GET" -> "-g":args
   "REMOVE" -> "-r":args
   _ -> ["Error"]
  where
    [t,d] = map ($str) [takeWhile (/='('),dropWhile (/='(') . takeWhile (/=')')]
    remwhite = concat . words
    com = remwhite $ map toUpper t
    args = splitOn ","  $ remwhite $ tail d

cdfix str =if null rest || null (tail rest) then [""] else [cd,tail rest]
  where
    (cd,rest) = splitAt 2 str
    